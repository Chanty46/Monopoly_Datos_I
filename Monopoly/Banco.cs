namespace Monopoly;

// En esta clase Banco orquestamos toda la partida, el tablero, turnos, compras, construcciones e intercambios
class Banco
{
    private Tablero TableroJuego;
    private ListaCircularCartas MazoEventos;
    private ListaTurnos TurnosJugadores;
    private Random random;

    public Banco()
    {
        TableroJuego = new Tablero();
        MazoEventos = MazoCartas.CrearMazoCartas();
        TurnosJugadores = new ListaTurnos();
        random = new Random();
        poblarTablero(TableroJuego); // Llenamos las 40 casillas del tablero oficial
    }

    // ================= GETTERS Y SETTERS =================
    public Tablero getTableroJuego() { return TableroJuego; }
    public ListaCircularCartas getMazoEventos() { return MazoEventos; }
    public ListaTurnos getTurnosJugadores() { return TurnosJugadores; }
    public int getCasasRestantes() { return TableroJuego.getCasasRestantes(); }
    public int getHotelesRestantes() { return TableroJuego.getHotelesRestantes(); }
    public void setCasasRestantes(int casas) { TableroJuego.setCasasRestantes(casas); }
    public void setHotelesRestantes(int hoteles) { TableroJuego.setHotelesRestantes(hoteles); }

    // ================= DADOS SIMULADOS =================

    // Simulamos tirar 2 dados normales de 6 caras y devolvemos la suma (de 2 a 12)
    public int TirarDados()
    {
        return random.Next(1, 7) + random.Next(1, 7);
    }

    // Aqui devolvemos cada dado por separado, util para la UI o ver si saco dobles
    public (int, int) TirarDadosDetallado()
    {
        return (random.Next(1, 7), random.Next(1, 7));
    }

    // ================= GESTION DE JUGADORES =================

    // Registramos al jugador en el tablero colocandolo en Salida (head) y lo metemos a la cola de turnos
    public void registrarJugador(Jugador nuevoJugador)
    {
        nuevoJugador.setNodoActual(TableroJuego.getHead());
        TurnosJugadores.agregarJugador(nuevoJugador);
    }

    public Jugador getJugadorActual()
    {
        return TurnosJugadores.getTurnoActual();
    }

    // ================= LOGICA DE TURNOS Y MOVIMIENTO =================
    public bool ProcesarTirada(Jugador jugador, int dados)
    {
        // 1. Si el jugador está encarcelado o perdiendo turnos
        if (jugador.getEstaEncarcelado())
        {
            int turnosRestantes = jugador.getTurnosPerdidos() - 1;
            jugador.setTurnosPerdidos(Math.Max(0, turnosRestantes));

            if (jugador.getTurnosPerdidos() == 0)
            {
                jugador.setEstaEncarcelado(false); // Ya cumplio sancion, queda libre para el proximo turno
            }
            return true; // No se mueve este turno por estar cumpliendo sanción
        }

        // 2. Mover ficha en el tablero y verificar si cruzó Salida (+200)
        bool pasoPorSalida = TableroJuego.moverJugadorPorDados(jugador, dados);
        if (pasoPorSalida)
        {
            jugador.setSaldo(jugador.getSaldo() + 200); // Cobra $200 por pasar por la salida
        }

        // 3. Evaluar la casilla donde cayó
        Casilla casillaDestino = jugador.getNodoActual().getCasilla();

        // Si es una propiedad sin dueño, no se cobra alquiler; queda disponible para compra
        if (casillaDestino is Propiedad propiedad && !propiedad.tieneDuenio())
        {
            return true;
        }

        // Si es la casilla de Salida, ya se le otorgaron los $200 por pasoPorSalida
        if (casillaDestino is CasillaInicial)
        {
            return true;
        }

        // En cualquier otra casilla, aplicar su efecto (cobrar renta, robar carta, etc.)
        bool resultadoCasilla = casillaDestino.aplicarCasilla(jugador);

        // Si no pudo pagar (alquiler, etc.), se procesa la bancarrota
        if (!resultadoCasilla)
        {
            if (casillaDestino is Propiedad propConDuenio && propConDuenio.tieneDuenio())
            {
                ManejarBancarrota(jugador, propConDuenio.getDuenio());
            }
            else
            {
                ManejarBancarrota(jugador, null);
            }
            return false;
        }

        // 4. Si la casilla causó encarcelamiento (ej. CasillaPolicia), reubicar en cárcel (ID 10)
        if (jugador.getEstaEncarcelado())
        {
            TableroJuego.fueEncarcelado(jugador);
        }

        return true;
    }

    public bool ProcesarCompra(Jugador jugador)
    {
        Casilla casillaActual = jugador.getNodoActual().getCasilla();

        if (casillaActual is Propiedad propiedad && !propiedad.tieneDuenio())
        {
            if (jugador.getSaldo() >= propiedad.getPrecioDeCompra())
            {
                propiedad.comprar(jugador);
                return true;
            }
        }
        return false;
    }

    public bool ProcesarNoCompra(Jugador jugador)
    {
        return true;
    }

    public void ProcesarFinTurno()
    {
        TurnosJugadores.avanzarTurno(); // Pasa la ficha al siguiente jugador en la cola
    }

    // ================= GESTION DE CONSTRUCCION Y MEJORAS =================

    // Metodo para construir casas u hoteles
    public bool ProcesarConstruirCasa(Jugador jugador, Propiedad propiedad)
    {
        // Validaciones basicas: que existan, que sea el duenio, que no este hipotecada ni tenga ya hotel
        if (jugador == null || propiedad == null) return false;
        if (propiedad.getDuenio() != jugador) return false;
        if (propiedad.getEstaHipotecada()) return false;
        if (propiedad.getTieneHotel()) return false;

        // Verificar si cumple con tener todo el grupo y la regla uniforme de construccion
        if (!jugador.getPropiedades().puedeMejorar(propiedad)) return false;

        int costo = propiedad.getCostoCasa();
        if (jugador.getSaldo() < costo) return false; // No le alcanza el dinero

        // Si ya tiene 4 casas, la siguiente mejora pasa a ser un Hotel!
        if (propiedad.getCasasPuestas() == 4)
        {
            if (TableroJuego.getHotelesRestantes() <= 0) return false; // El banco se quedo sin hoteles

            // Al colocar hotel, se devuelven 4 casas al banco y se consume 1 hotel
            bool exito = propiedad.agregarCasa();
            if (exito)
            {
                jugador.setSaldo(jugador.getSaldo() - costo);
                TableroJuego.setCasasRestantes(TableroJuego.getCasasRestantes() + 4);
                TableroJuego.setHotelesRestantes(TableroJuego.getHotelesRestantes() - 1);
                return true;
            }
        }
        else
        {
            // Caso normal: construir una casa (de 0 a 3 pasando a 1 a 4)
            if (TableroJuego.getCasasRestantes() <= 0) return false; // El banco se quedo sin casas

            bool exito = propiedad.agregarCasa();
            if (exito)
            {
                jugador.setSaldo(jugador.getSaldo() - costo);
                TableroJuego.setCasasRestantes(TableroJuego.getCasasRestantes() - 1);
                return true;
            }
        }

        return false;
    }

    // Metodo para vender casas y recuperar el 50% de la inversion
    public bool ProcesarVenderCasa(Jugador jugador, Propiedad propiedad)
    {
        if (jugador == null || propiedad == null) return false;
        if (propiedad.getDuenio() != jugador) return false;
        if (propiedad.getEstaHipotecada()) return false;
        if (propiedad.getCasasPuestas() == 0 && !propiedad.getTieneHotel()) return false; // No tiene nada que vender

        // Verificar si puede quitar casa segun la regla de nivelacion del grupo
        if (!jugador.getPropiedades().puedeQuitarCasa(propiedad)) return false;

        int reembolso = propiedad.getCostoCasa() / 2; // Monopoly devuelve la mitad del costo de la casa

        if (propiedad.getTieneHotel())
        {
            // Al vender un hotel, pasa a tener 4 casas (se necesitan 4 casas en el stock del tablero)
            if (TableroJuego.getCasasRestantes() < 4) return false;

            bool exito = propiedad.removerCasa();
            if (exito)
            {
                jugador.setSaldo(jugador.getSaldo() + reembolso);
                TableroJuego.setHotelesRestantes(TableroJuego.getHotelesRestantes() + 1);
                TableroJuego.setCasasRestantes(TableroJuego.getCasasRestantes() - 4);
                return true;
            }
        }
        else
        {
            // Vender una casa normal
            bool exito = propiedad.removerCasa();
            if (exito)
            {
                jugador.setSaldo(jugador.getSaldo() + reembolso);
                TableroJuego.setCasasRestantes(TableroJuego.getCasasRestantes() + 1); // Regresa 1 casa al banco
                return true;
            }
        }

        return false;
    }

    // Hipotecar: solo si no tiene casas puestas ni hotel
    public bool ProcesarHipotecar(Jugador jugador, Propiedad propiedad)
    {
        if (jugador == null || propiedad == null) return false;
        if (propiedad.getDuenio() != jugador) return false;
        if (propiedad.getEstaHipotecada()) return false;
        if (propiedad.getCasasPuestas() > 0 || propiedad.getTieneHotel()) return false; // No se puede hipotecar con casas

        propiedad.hipotecar();
        return propiedad.getEstaHipotecada();
    }

    // Deshipotecar: cobra la hipoteca mas el 10% de interes oficial
    public bool ProcesarDeshipotecar(Jugador jugador, Propiedad propiedad)
    {
        if (jugador == null || propiedad == null) return false;
        if (propiedad.getDuenio() != jugador) return false;
        if (!propiedad.getEstaHipotecada()) return false;

        int costo = (propiedad.getPrecioDeCompra() / 2) + (propiedad.getPrecioDeCompra() / 10);
        if (jugador.getSaldo() < costo) return false; // Saldo insuficiente

        propiedad.desHipotecar();
        return !propiedad.getEstaHipotecada();
    }

    // ================= INTERCAMBIOS (TRADE) =================

    // Sistema de trade entre dos jugadores: intercambian listas de propiedades y dinero de forma segura
    public bool ProcesarIntercambio(Jugador jugadorA, ListaPropiedades propiedadesA, int dineroA, Jugador jugadorB, ListaPropiedades propiedadesB, int dineroB)
    {
        // 1. Validaciones basicas: jugadores validos, activos y con saldo suficiente
        if (jugadorA == null || jugadorB == null || jugadorA == jugadorB) return false;
        if (!jugadorA.isActivo() || !jugadorB.isActivo()) return false;
        if (dineroA < 0 || dineroB < 0) return false;
        if (jugadorA.getSaldo() < dineroA || jugadorB.getSaldo() < dineroB) return false;

        // 2. Validar propiedades ofrecidas por Jugador A (no pueden tener casas ni hoteles)
        if (propiedadesA != null)
        {
            NodoPropiedad? actualA = propiedadesA.getHead();
            while (actualA != null)
            {
                Propiedad prop = actualA.getPropiedad();
                if (prop == null) return false;
                if (prop.getDuenio() != jugadorA) return false;
                // No se pueden intercambiar propiedades con casas u hoteles
                if (prop.getCasasPuestas() > 0 || prop.getTieneHotel()) return false;
                actualA = actualA.getSiguiente();
            }
        }

        // 2. Validar propiedades ofrecidas por Jugador B (no pueden tener casas ni hoteles)
        if (propiedadesB != null)
        {
            NodoPropiedad? actualB = propiedadesB.getHead();
            while (actualB != null)
            {
                Propiedad prop = actualB.getPropiedad();
                if (prop == null) return false;
                if (prop.getDuenio() != jugadorB) return false;
                // No se pueden intercambiar propiedades con casas u hoteles
                if (prop.getCasasPuestas() > 0 || prop.getTieneHotel()) return false;
                actualB = actualB.getSiguiente();
            }
        }

        // 3. Ejecutar transferencia de dinero entre ambos
        jugadorA.setSaldo(jugadorA.getSaldo() - dineroA + dineroB);
        jugadorB.setSaldo(jugadorB.getSaldo() - dineroB + dineroA);

        // 4. Traspasar propiedades de A hacia B
        if (propiedadesA != null)
        {
            NodoPropiedad? actualA = propiedadesA.getHead();
            while (actualA != null)
            {
                Propiedad prop = actualA.getPropiedad();
                jugadorA.getPropiedades().eliminarPropiedad(prop);
                prop.setDuenio(jugadorB);
                jugadorB.getPropiedades().agregarPropiedad(prop);
                actualA = actualA.getSiguiente();
            }
        }

        // 5. Traspasar propiedades de B hacia A
        if (propiedadesB != null)
        {
            NodoPropiedad? actualB = propiedadesB.getHead();
            while (actualB != null)
            {
                Propiedad prop = actualB.getPropiedad();
                jugadorB.getPropiedades().eliminarPropiedad(prop);
                prop.setDuenio(jugadorA);
                jugadorA.getPropiedades().agregarPropiedad(prop);
                actualB = actualB.getSiguiente();
            }
        }

        return true;
    }

    // ================= BANCARROTA Y GANADOR =================
    public void ManejarBancarrota(Jugador quebrado, Jugador? acreedor)
    {
        quebrado.setActivo(false);
        quebrado.setEnBancarrota(true);

        if (acreedor != null)
        {
            // Traspasar el saldo restante positivo al acreedor
            if (quebrado.getSaldo() > 0)
            {
                acreedor.setSaldo(acreedor.getSaldo() + quebrado.getSaldo());
                quebrado.setSaldo(0);
            }

            // Traspasar todas las propiedades al acreedor
            NodoPropiedad? actual = quebrado.getPropiedades().getHead();
            while (actual != null)
            {
                Propiedad prop = actual.getPropiedad();
                prop.setDuenio(acreedor);
                acreedor.getPropiedades().agregarPropiedad(prop);
                actual = actual.getSiguiente();
            }
        }
        else
        {
            // Si el acreedor es el banco, las propiedades vuelven a quedar libres
            NodoPropiedad? actual = quebrado.getPropiedades().getHead();
            while (actual != null)
            {
                Propiedad prop = actual.getPropiedad();
                prop.setDuenio(null!);
                prop.setEstaHipotecada(false);
                prop.setCasasPuestas(0);
                prop.setTieneHotel(false);
                actual = actual.getSiguiente();
            }
        }

        // Limpiar propiedades del jugador eliminado
        quebrado.getPropiedades().setHead(null!);
        quebrado.getPropiedades().setSize(0);

        // Remover de la cola de turnos
        TurnosJugadores.eliminarJugadorBancarrota(quebrado);
    }

    public Jugador? verificarGanador()
    {
        if (TurnosJugadores.haySoloUnJugador())
        {
            return TurnosJugadores.getTurnoActual(); // Si solo queda uno en la cola, tenemos ganador
        }
        return null;
    }

    public string ObtenerEstado()
    {
        string estado = "=== ESTADO DEL JUEGO ===\n";
        estado += $"Jugadores restantes: {TurnosJugadores.getCantidadJugadores()}\n";
        estado += $"Casas en stock banco: {TableroJuego.getCasasRestantes()} | Hoteles en stock banco: {TableroJuego.getHotelesRestantes()}\n";
        Jugador actual = TurnosJugadores.getTurnoActual();
        if (actual != null)
        {
            estado += $"Turno actual: {actual.getNombre()} (ID: {actual.getID()}) | Saldo: ${actual.getSaldo()} | Posición: {actual.getNodoActual().getCasilla().getNombre()} (ID: {actual.getNodoActual().getCasilla().getIdCasilla()})\n";
        }
        return estado;
    }

    // ================= POBLAR TABLERO =================
    // 40 casillas del Monopoly real, en orden (ID = posicion en el tablero)
    // Convencion: precios multiplos de 10, alquiler base del Monopoly estandar
    private void poblarTablero(Tablero tablero)
    {
        // --- LADO 1: Salida → Carcel ---
        tablero.agregarCasilla(new CasillaInicial("Salida", 0));
        tablero.agregarCasilla(new Propiedad("Mediterraneo", 1, 60, 2, "Cafe", 2));
        tablero.agregarCasilla(new CasillaEvento("Comunidad", 2, MazoEventos));
        tablero.agregarCasilla(new Propiedad("Baltico", 3, 60, 4, "Cafe", 2));
        tablero.agregarCasilla(new CasillaBanco("Impuesto Renta", 4));
        tablero.agregarCasilla(new Ferrocarril("FC Reading", 5, 200));
        tablero.agregarCasilla(new Propiedad("Oriental", 6, 100, 6, "Celeste", 3));
        tablero.agregarCasilla(new CasillaEvento("Suerte", 7, MazoEventos));
        tablero.agregarCasilla(new Propiedad("Vermont", 8, 100, 6, "Celeste", 3));
        tablero.agregarCasilla(new Propiedad("Connecticut", 9, 120, 8, "Celeste", 3));
        tablero.agregarCasilla(new CasillaCarcel("Carcel", 10));

        // --- LADO 2: Cárcel → Parqueo Libre ---
        tablero.agregarCasilla(new Propiedad("St. Charles", 11, 140, 10, "Rosa", 3));
        tablero.agregarCasilla(new CasillaServicio("Cia. Electrica", 12, 150));
        tablero.agregarCasilla(new Propiedad("States Ave", 13, 140, 10, "Rosa", 3));
        tablero.agregarCasilla(new Propiedad("Virginia Ave", 14, 160, 12, "Rosa", 3));
        tablero.agregarCasilla(new Ferrocarril("FC Pennsylvania", 15, 200));
        tablero.agregarCasilla(new Propiedad("St. James", 16, 180, 14, "Naranja", 3));
        tablero.agregarCasilla(new CasillaEvento("Comunidad", 17, MazoEventos));
        tablero.agregarCasilla(new Propiedad("Tennessee Ave", 18, 180, 14, "Naranja", 3));
        tablero.agregarCasilla(new Propiedad("New York Ave", 19, 200, 16, "Naranja", 3));
        tablero.agregarCasilla(new CasillaParqueoLibre("Parqueo Libre", 20));

        // --- LADO 3: Parqueo Libre → Vaya a la Carcel ---
        tablero.agregarCasilla(new Propiedad("Kentucky Ave", 21, 220, 18, "Rojo", 3));
        tablero.agregarCasilla(new CasillaEvento("Suerte", 22, MazoEventos));
        tablero.agregarCasilla(new Propiedad("Indiana Ave", 23, 220, 18, "Rojo", 3));
        tablero.agregarCasilla(new Propiedad("Illinois Ave", 24, 240, 20, "Rojo", 3));
        tablero.agregarCasilla(new Ferrocarril("FC B&O", 25, 200));
        tablero.agregarCasilla(new Propiedad("Atlantic Ave", 26, 260, 22, "Amarillo", 3));
        tablero.agregarCasilla(new Propiedad("Ventnor Ave", 27, 260, 22, "Amarillo", 3));
        tablero.agregarCasilla(new CasillaServicio("Aguas", 28, 150));
        tablero.agregarCasilla(new Propiedad("Marvin Gardens", 29, 280, 24, "Amarillo", 3));
        tablero.agregarCasilla(new CasillaPolicia("Vaya a la Carcel", 30));

        // --- LADO 4: Cárcel → Salida ---
        tablero.agregarCasilla(new Propiedad("Pacific Ave", 31, 300, 26, "Verde", 3));
        tablero.agregarCasilla(new Propiedad("North Carolina Ave", 32, 300, 26, "Verde", 3));
        tablero.agregarCasilla(new CasillaEvento("Comunidad", 33, MazoEventos));
        tablero.agregarCasilla(new Propiedad("Pennsylvania Ave", 34, 320, 28, "Verde", 3));
        tablero.agregarCasilla(new Ferrocarril("FC Short Line", 35, 200));
        tablero.agregarCasilla(new CasillaEvento("Suerte", 36, MazoEventos));
        tablero.agregarCasilla(new Propiedad("Park Place", 37, 350, 35, "AzulOscuro", 2));
        tablero.agregarCasilla(new CasillaBanco("Impuesto Lujo", 38));
        tablero.agregarCasilla(new Propiedad("Boardwalk", 39, 400, 50, "AzulOscuro", 2));
    }
}