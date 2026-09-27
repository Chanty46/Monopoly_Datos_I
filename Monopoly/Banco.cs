namespace Monopoly;

class Banco
{
    private Tablero TableroJuego;
    private ListaCircularCartas MazoEventos;
    private ListaTurnos TurnosJugadores;

    public Banco()
    {
        TableroJuego = new Tablero();
        MazoEventos = MazoCartas.CrearMazoCartas();
        TurnosJugadores = new ListaTurnos();
        poblarTablero(TableroJuego);
    }

    // ================= GETTERS =================
    public Tablero getTableroJuego() { return TableroJuego; }
    public ListaCircularCartas getMazoEventos() { return MazoEventos; }
    public ListaTurnos getTurnosJugadores() { return TurnosJugadores; }

    // ================= GESTION DE JUGADORES =================
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
                jugador.setEstaEncarcelado(false);
            }
            return true; // No se mueve este turno por estar cumpliendo sanción
        }

        // 2. Mover ficha en el tablero y verificar si cruzó Salida (+200)
        bool pasoPorSalida = TableroJuego.moverJugadorPorDados(jugador, dados);
        if (pasoPorSalida)
        {
            jugador.setSaldo(jugador.getSaldo() + 200);
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

        // En cualquier otra casilla, aplicar su efecto
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
        TurnosJugadores.avanzarTurno();
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
            return TurnosJugadores.getTurnoActual();
        }
        return null;
    }

    public string ObtenerEstado()
    {
        string estado = "=== ESTADO DEL JUEGO ===\n";
        estado += $"Jugadores restantes: {TurnosJugadores.getCantidadJugadores()}\n";
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

        // ID 0
        tablero.agregarCasilla(new CasillaInicial("Salida", 0));

        // ID 1 - Mediterráneo (Café, grupo de 2)
        tablero.agregarCasilla(new Propiedad("Mediterraneo", 1, 60, 2, "Cafe", 2));

        // ID 2 - Comunidad (carta de evento)
        tablero.agregarCasilla(new CasillaEvento("Comunidad", 2, MazoEventos));

        // ID 3 - Báltico (Café, grupo de 2)
        tablero.agregarCasilla(new Propiedad("Baltico", 3, 60, 4, "Cafe", 2));

        // ID 4 - Impuesto sobre la Renta (el banco cobra $200)
        tablero.agregarCasilla(new CasillaBanco("Impuesto Renta", 4));

        // ID 5 - Ferrocarril Reading
        tablero.agregarCasilla(new Ferrocarril("FC Reading", 5, 200));

        // ID 6 - Oriental (Celeste, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Oriental", 6, 100, 6, "Celeste", 3));

        // ID 7 - Suerte (carta de evento)
        tablero.agregarCasilla(new CasillaEvento("Suerte", 7, MazoEventos));

        // ID 8 - Vermont (Celeste, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Vermont", 8, 100, 6, "Celeste", 3));

        // ID 9 - Connecticut (Celeste, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Connecticut", 9, 120, 8, "Celeste", 3));

        // ID 10 - Cárcel / Solo de visita
        tablero.agregarCasilla(new CasillaCarcel("Carcel", 10));

        // --- LADO 2: Cárcel → Parqueo Libre ---

        // ID 11 - St. Charles (Rosa, grupo de 3)
        tablero.agregarCasilla(new Propiedad("St. Charles", 11, 140, 10, "Rosa", 3));

        // ID 12 - Compañía Eléctrica (Servicio, grupo de 2)
        tablero.agregarCasilla(new CasillaServicio("Cia. Electrica", 12, 150));

        // ID 13 - States Ave (Rosa, grupo de 3)
        tablero.agregarCasilla(new Propiedad("States Ave", 13, 140, 10, "Rosa", 3));
        // ID 14 - Virginia Ave (Rosa, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Virginia Ave", 14, 160, 12, "Rosa", 3));
        // ID 15 - Ferrocarril Pennsylvania
        tablero.agregarCasilla(new Ferrocarril("FC Pennsylvania", 15, 200));
        // ID 16 - St. James (Naranja, grupo de 3)
        tablero.agregarCasilla(new Propiedad("St. James", 16, 180, 14, "Naranja", 3));
        // ID 17 - Comunidad
        tablero.agregarCasilla(new CasillaEvento("Comunidad", 17, MazoEventos));
        // ID 18 - Tennessee Ave (Naranja, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Tennessee Ave", 18, 180, 14, "Naranja", 3));
        // ID 19 - New York Ave (Naranja, grupo de 3)
        tablero.agregarCasilla(new Propiedad("New York Ave", 19, 200, 16, "Naranja", 3));
        // ID 20 - Parqueo Libre
        tablero.agregarCasilla(new CasillaParqueoLibre("Parqueo Libre", 20));

        // --- LADO 3: Parqueo Libre → Vaya a la Carcel ---

        // ID 21 - Kentucky Ave (Rojo, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Kentucky Ave", 21, 220, 18, "Rojo", 3));
        // ID 22 - Suerte
        tablero.agregarCasilla(new CasillaEvento("Suerte", 22, MazoEventos));
        // ID 23 - Indiana Ave (Rojo, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Indiana Ave", 23, 220, 18, "Rojo", 3));
        // ID 24 - Illinois Ave (Rojo, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Illinois Ave", 24, 240, 20, "Rojo", 3));
        // ID 25 - Ferrocarril B&O
        tablero.agregarCasilla(new Ferrocarril("FC B&O", 25, 200));
        // ID 26 - Atlantic Ave (Amarillo, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Atlantic Ave", 26, 260, 22, "Amarillo", 3));
        // ID 27 - Ventnor Ave (Amarillo, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Ventnor Ave", 27, 260, 22, "Amarillo", 3));
        // ID 28 - Empresa de Aguas (Servicio, grupo de 2)
        tablero.agregarCasilla(new CasillaServicio("Aguas", 28, 150));
        // ID 29 - Marvin Gardens (Amarillo, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Marvin Gardens", 29, 280, 24, "Amarillo", 3));
        // ID 30 - Vaya a la Cárcel
        tablero.agregarCasilla(new CasillaPolicia("Vaya a la Carcel", 30));

        // --- LADO 4: Cárcel → Salida ---

        // ID 31 - Pacific Ave (Verde, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Pacific Ave", 31, 300, 26, "Verde", 3));
        // ID 32 - North Carolina Ave (Verde, grupo de 3)
        tablero.agregarCasilla(new Propiedad("North Carolina Ave", 32, 300, 26, "Verde", 3));
        // ID 33 - Comunidad
        tablero.agregarCasilla(new CasillaEvento("Comunidad", 33, MazoEventos));
        // ID 34 - Pennsylvania Ave (Verde, grupo de 3)
        tablero.agregarCasilla(new Propiedad("Pennsylvania Ave", 34, 320, 28, "Verde", 3));
        // ID 35 - Ferrocarril Short Line
        tablero.agregarCasilla(new Ferrocarril("FC Short Line", 35, 200));
        // ID 36 - Suerte
        tablero.agregarCasilla(new CasillaEvento("Suerte", 36, MazoEventos));
        // ID 37 - Park Place (Azul Oscuro, grupo de 2)
        tablero.agregarCasilla(new Propiedad("Park Place", 37, 350, 35, "AzulOscuro", 2));
        // ID 38 - Impuesto de Lujo (el banco cobra $100)
        tablero.agregarCasilla(new CasillaBanco("Impuesto Lujo", 38));
        // ID 39 - Boardwalk (Azul Oscuro, grupo de 2)
        tablero.agregarCasilla(new Propiedad("Boardwalk", 39, 400, 50, "AzulOscuro", 2));
    }
}