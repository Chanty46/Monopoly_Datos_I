namespace Monopoly;

public static class PruebasUnitariasLogica
{
    // Variables para estadísticas de pruebas
    private static int totalPruebas = 0;
    private static int pruebasExitosas = 0;

    public static void EjecutarPruebas()
    {
        totalPruebas = 0;
        pruebasExitosas = 0;

        Console.WriteLine("================================================================================");
        Console.WriteLine("        SUITE DE PRUEBAS UNITARIAS - MONOPOLY (VERIFICACION DE METODOS)         ");
        Console.WriteLine("================================================================================\n");

        ProbarCasillaBase();
        ProbarCasillasEspeciales();
        ProbarJugador();
        ProbarPropiedad();
        ProbarListaPropiedades();
        ProbarTablero();
        ProbarBanco();

        Console.WriteLine("\n================================================================================");
        Console.WriteLine($" RESUMEN FINAL: {pruebasExitosas}/{totalPruebas} pruebas superadas con exito.");
        Console.WriteLine("================================================================================");
    }

    // Metodo auxiliar para registrar y verificar el resultado de cada prueba
    private static void Verificar(string nombrePrueba, bool condicion, string detalles = "")
    {
        totalPruebas++;
        if (condicion)
        {
            pruebasExitosas++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  [EXITO] ");
            Console.ResetColor();
            Console.WriteLine($"{nombrePrueba} {detalles}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  [FALLO] ");
            Console.ResetColor();
            Console.WriteLine($"{nombrePrueba} - ERROR: La condicion no se cumplio. {detalles}");
        }
    }

    // ============================================================================
    // 1. PRUEBAS DE LA CLASE BASE CASILLA
    // ============================================================================
    private static void ProbarCasillaBase()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- [SECCION 1: PRUEBAS DE CASILLA BASE] ---");
        Console.ResetColor();

        // Prueba 1.1: Verificacion de constructor y getters de Casilla
        // Crea una casilla basica y confirma que nombre e ID se asignaron correctamente.
        Casilla c = new Casilla("Avenida Central", 10);
        Verificar("1.1 Constructor y Getters Casilla", 
            c.getNombre() == "Avenida Central" && c.getIdCasilla() == 10,
            $"=> Nombre: '{c.getNombre()}', ID: {c.getIdCasilla()}");

        // Prueba 1.2: Verificacion de setters de Casilla
        // Modifica nombre e ID y comprueba que se actualicen los valores.
        c.setNombre("Nueva Avenida Central");
        c.setIdCasilla(25);
        Verificar("1.2 Setters Casilla", 
            c.getNombre() == "Nueva Avenida Central" && c.getIdCasilla() == 25,
            $"=> Nuevo Nombre: '{c.getNombre()}', Nuevo ID: {c.getIdCasilla()}");

        // Prueba 1.3: Verificacion del metodo virtual aplicarCasilla
        // En la clase base, aplicarCasilla() no debe alterar ningun atributo del jugador.
        Jugador testJugador = new Jugador(1, "JugadorPrueba", null!);
        int saldoAntes = testJugador.getSaldo();
        c.aplicarCasilla(testJugador);
        Verificar("1.3 aplicarCasilla en Casilla Base (vacio)", 
            testJugador.getSaldo() == saldoAntes,
            $"=> Saldo sin cambios: {testJugador.getSaldo()}");
        Console.WriteLine();
    }

    // ============================================================================
    // 2. PRUEBAS DE CASILLAS ESPECIALES (POLIMORFISMO Y HERENCIA)
    // ============================================================================
    private static void ProbarCasillasEspeciales()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- [SECCION 2: PRUEBAS DE CASILLAS ESPECIALES] ---");
        Console.ResetColor();

        Jugador jugador = new Jugador(1, "Carlos", null!);

        // Prueba 2.1: CasillaInicial - Debe sumar $200 al saldo del jugador
        CasillaInicial salida = new CasillaInicial("Salida", 0);
        int saldoAntesSalida = jugador.getSaldo(); // Inicia en 200
        salida.aplicarCasilla(jugador);
        Verificar("2.1 CasillaInicial (+200 saldo)", 
            jugador.getSaldo() == saldoAntesSalida + 200,
            $"=> Saldo anterior: {saldoAntesSalida}, Saldo posterior: {jugador.getSaldo()}");

        // Prueba 2.2: CasillaPolicia - Debe marcar encarcelado=true y turnosPerdidos=3
        CasillaPolicia policia = new CasillaPolicia("Vaya a la Carcel", 30);
        policia.aplicarCasilla(jugador);
        Verificar("2.2 CasillaPolicia (encarcelar y asignar 3 turnos)", 
            jugador.getEstaEncarcelado() == true && jugador.getTurnosPerdidos() == 3,
            $"=> Encarcelado: {jugador.getEstaEncarcelado()}, Turnos perdidos: {jugador.getTurnosPerdidos()}");

        // Prueba 2.3: CasillaCarcel cuando esta encarcelado - Debe decrementar un turno perdido
        CasillaCarcel carcel = new CasillaCarcel("Carcel", 1);
        int turnosAntes = jugador.getTurnosPerdidos(); // Tiene 3
        carcel.aplicarCasilla(jugador);
        Verificar("2.3 CasillaCarcel (reducir turno si esta encarcelado)", 
            jugador.getTurnosPerdidos() == turnosAntes - 1,
            $"=> Turnos anteriores: {turnosAntes}, Turnos actuales: {jugador.getTurnosPerdidos()}");

        // Prueba 2.4: CasillaCarcel en modo visita (estaEncarcelado = false) - No debe reducir turnos
        jugador.setEstaEncarcelado(false);
        jugador.setTurnosPerdidos(0);
        carcel.aplicarCasilla(jugador);
        Verificar("2.4 CasillaCarcel en visita (no altera turnos)", 
            jugador.getTurnosPerdidos() == 0,
            $"=> Turnos perdidos: {jugador.getTurnosPerdidos()}");

        // Prueba 2.5: CasillaParqueoLibre - Casilla de descanso, no debe modificar estado del jugador
        CasillaParqueoLibre parking = new CasillaParqueoLibre("Parqueo Libre", 20);
        int saldoAntesParking = jugador.getSaldo();
        parking.aplicarCasilla(jugador);
        Verificar("2.5 CasillaParqueoLibre (no altera estado del jugador)", 
            jugador.getSaldo() == saldoAntesParking,
            $"=> Saldo conservado: {jugador.getSaldo()}");

        // Prueba 2.6: CasillaEvento - Verificacion de inicializacion
        CasillaEvento evento = new CasillaEvento("Suerte", 7, null!);
        Verificar("2.6 CasillaEvento (inicializacion basica)", 
            evento.getNombre() == "Suerte" && evento.getIdCasilla() == 7,
            $"=> Nombre: {evento.getNombre()}, ID: {evento.getIdCasilla()}");
        Console.WriteLine();
    }

    // ============================================================================
    // 3. PRUEBAS DE LA CLASE JUGADOR
    // ============================================================================
    private static void ProbarJugador()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- [SECCION 3: PRUEBAS DE LA CLASE JUGADOR] ---");
        Console.ResetColor();

        // Prueba 3.1: Constructor y valores por defecto de Jugador
        // Saldo inicial = 200, turnosPerdidos = 0, activo = true, estaEncarcelado = false, lista propiedades vacia
        Casilla c0 = new Casilla("Salida", 0);
        NodoCasilla nodo0 = new NodoCasilla(c0);
        Jugador jugador = new Jugador(1, "Sofia", nodo0);

        bool initCorrecto = jugador.getID() == 1 &&
                             jugador.getNombre() == "Sofia" &&
                             jugador.getSaldo() == 200 &&
                             jugador.getTurnosPerdidos() == 0 &&
                             jugador.isActivo() == true &&
                             jugador.getEstaEncarcelado() == false &&
                             jugador.getNodoActual() == nodo0 &&
                             jugador.getPropiedades() != null;

        Verificar("3.1 Constructor de Jugador (valores por defecto)", initCorrecto,
            $"=> ID: {jugador.getID()}, Nombre: {jugador.getNombre()}, Saldo: {jugador.getSaldo()}, Activo: {jugador.isActivo()}");

        // Prueba 3.2: Setters de Jugador (saldo, turnos, activo, encarcelado, nodo)
        jugador.setSaldo(1500);
        jugador.setTurnosPerdidos(2);
        jugador.setActivo(false);
        jugador.setEstaEncarcelado(true);

        Casilla c1 = new Casilla("Destino", 5);
        NodoCasilla nodo1 = new NodoCasilla(c1);
        jugador.setNodoActual(nodo1);

        bool settersCorrectos = jugador.getSaldo() == 1500 &&
                               jugador.getTurnosPerdidos() == 2 &&
                               jugador.isActivo() == false &&
                               jugador.getEstaEncarcelado() == true &&
                               jugador.getNodoActual() == nodo1;

        Verificar("3.2 Setters de Jugador", settersCorrectos,
            $"=> Saldo: {jugador.getSaldo()}, Turnos: {jugador.getTurnosPerdidos()}, Activo: {jugador.isActivo()}, Encarcelado: {jugador.getEstaEncarcelado()}");
        Console.WriteLine();
    }

    // ============================================================================
    // 4. PRUEBAS DE LA CLASE PROPIEDAD
    // ============================================================================
    private static void ProbarPropiedad()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- [SECCION 4: PRUEBAS DE LA CLASE PROPIEDAD] ---");
        Console.ResetColor();

        // Propiedad(nombre, id, precio, alquiler, grupo, grupoSize)
        Propiedad propAzul = new Propiedad("Paseo del Prado", 39, 400, 50, "AzulOscuro", 2);

        // Prueba 4.1: Verificacion de constructor y getters iniciales
        bool propInit = propAzul.getPrecioDeCompra() == 400 &&
                        propAzul.getAlquiler() == 50 &&
                        propAzul.getGrupo() == "AzulOscuro" &&
                        propAzul.getGrupoSize() == 2 &&
                        propAzul.getDuenio() == null &&
                        propAzul.getCasasPuestas() == 0 &&
                        propAzul.getTieneHotel() == false &&
                        propAzul.getEstaHipotecada() == false &&
                        propAzul.tieneDuenio() == false;

        Verificar("4.1 Constructor y Getters de Propiedad", propInit,
            $"=> Precio: {propAzul.getPrecioDeCompra()}, Alquiler: {propAzul.getAlquiler()}, Grupo: {propAzul.getGrupo()}, TamGrupo: {propAzul.getGrupoSize()}");

        // Prueba 4.2: Setters de Propiedad (atributos modificables en juego)
        propAzul.setCasasPuestas(3);
        propAzul.setTieneHotel(true);
        propAzul.setAlquiler(100);
        propAzul.setPrecioDeCompra(420);

        bool settersOk = propAzul.getCasasPuestas() == 3 &&
                         propAzul.getTieneHotel() == true &&
                         propAzul.getAlquiler() == 100 &&
                         propAzul.getPrecioDeCompra() == 420;

        Verificar("4.2 Setters de Propiedad", settersOk,
            $"=> Casas: {propAzul.getCasasPuestas()}, Hotel: {propAzul.getTieneHotel()}, Alquiler: {propAzul.getAlquiler()}");

        // Restaurar estado para las siguientes pruebas
        propAzul.setCasasPuestas(0);
        propAzul.setTieneHotel(false);
        propAzul.setAlquiler(50);
        propAzul.setPrecioDeCompra(400);

        // Prueba 4.3: Metodo comprar() con fondos insuficientes
        // Jugador con $100 intenta comprar propiedad de $400. No debe poder comprarla.
        Jugador pobre = new Jugador(2, "Mateo", null!);
        pobre.setSaldo(100);
        propAzul.comprar(pobre);
        Verificar("4.3 comprar() con saldo insuficiente", 
            propAzul.tieneDuenio() == false && pobre.getSaldo() == 100,
            $"=> Tiene duenio: {propAzul.tieneDuenio()}, Saldo comprador: {pobre.getSaldo()}");

        // Prueba 4.4: Metodo comprar() con fondos suficientes
        // Jugador con $500 compra propiedad de $400. Saldo final debe ser $100 y duenio asignado.
        Jugador rico = new Jugador(3, "Laura", null!);
        rico.setSaldo(500);
        propAzul.comprar(rico);
        Verificar("4.4 comprar() con saldo suficiente", 
            propAzul.tieneDuenio() == true && 
            propAzul.getDuenio() == rico && 
            rico.getSaldo() == 100 &&
            rico.getPropiedades().calcularValorTotal() == 400,
            $"=> Duenio: {propAzul.getDuenio().getNombre()}, Saldo restante: {rico.getSaldo()}, Valor Propiedades: {rico.getPropiedades().calcularValorTotal()}");

        // Prueba 4.5: Metodo comprar() en propiedad ya comprada
        // Otro jugador con $1000 intenta comprar la misma propiedad. No debe poder y su saldo queda intacto.
        Jugador rival = new Jugador(4, "Andres", null!);
        rival.setSaldo(1000);
        propAzul.comprar(rival);
        Verificar("4.5 comprar() sobre propiedad que ya tiene duenio", 
            propAzul.getDuenio() == rico && rival.getSaldo() == 1000,
            $"=> El duenio sigue siendo: {propAzul.getDuenio().getNombre()}, Saldo rival intacto: {rival.getSaldo()}");

        // Prueba 4.6: Metodo cobrarAlquiler() normal
        // Rival con $1000 cae en propAzul (alquiler $50). Rival debe quedar en $950 y Rico en $100 + $50 = $150.
        propAzul.cobrarAlquiler(rival);
        Verificar("4.6 cobrarAlquiler() a inquilino", 
            rival.getSaldo() == 950 && rico.getSaldo() == 150,
            $"=> Saldo inquilino: {rival.getSaldo()}, Saldo duenio: {rico.getSaldo()}");

        // Prueba 4.7: Metodo cobrarAlquiler() cuando el jugador es el duenio
        // El duenio no debe cobrarse a si mismo.
        int saldoDuenioAntes = rico.getSaldo();
        propAzul.cobrarAlquiler(rico);
        Verificar("4.7 cobrarAlquiler() al propio duenio (no cobra)", 
            rico.getSaldo() == saldoDuenioAntes,
            $"=> Saldo duenio sin alteracion: {rico.getSaldo()}");

        // Prueba 4.8: Metodo hipotecar() sin casas ni hotel
        // Hipotecar otorga la mitad del precio (400 / 2 = 200) al duenio y marca estaHipotecada=true
        int saldoPrevioHipoteca = rico.getSaldo(); // 150
        propAzul.hipotecar();
        Verificar("4.8 hipotecar() sin casas/hotel", 
            propAzul.getEstaHipotecada() == true && rico.getSaldo() == saldoPrevioHipoteca + 200,
            $"=> Hipotecada: {propAzul.getEstaHipotecada()}, Saldo duenio: {rico.getSaldo()} (+200)");

        // Prueba 4.9: Metodo cobrarAlquiler() en propiedad hipotecada
        // Propiedad hipotecada no debe cobrar renta.
        int saldoRivalAntes = rival.getSaldo();
        int saldoRicoAntes = rico.getSaldo();
        propAzul.cobrarAlquiler(rival);
        Verificar("4.9 cobrarAlquiler() en propiedad hipotecada (no cobra)", 
            rival.getSaldo() == saldoRivalAntes && rico.getSaldo() == saldoRicoAntes,
            $"=> Saldo inquilino intacto: {rival.getSaldo()}, Saldo duenio intacto: {rico.getSaldo()}");

        // Prueba 4.10: Intentar hipotecar cuando tiene casas construidas (no debe permitirse)
        Propiedad propConCasas = new Propiedad("Gran Via", 15, 200, 20, "Rojo", 3);
        Jugador duenio2 = new Jugador(5, "Daniel", null!);
        duenio2.setSaldo(1000);
        propConCasas.comprar(duenio2);
        propConCasas.setCasasPuestas(2);
        int saldoDuenio2 = duenio2.getSaldo();
        propConCasas.hipotecar();
        Verificar("4.10 hipotecar() con casas puestas (no se permite)", 
            propConCasas.getEstaHipotecada() == false && duenio2.getSaldo() == saldoDuenio2,
            $"=> Hipotecada: {propConCasas.getEstaHipotecada()}, Saldo duenio: {duenio2.getSaldo()}");

        // Prueba 4.11: Metodo desHipotecar() con saldo suficiente
        // Costo = (precio/2 + precio/10) = (400/2 + 40) = 240. Rico tiene 350 (> 240).
        // Saldo posterior = 350 - 240 = 110. Hipotecada pasa a false.
        int saldoAntesDeshipotecar = rico.getSaldo(); // 350
        propAzul.desHipotecar();
        Verificar("4.11 desHipotecar() con saldo suficiente", 
            propAzul.getEstaHipotecada() == false && rico.getSaldo() == saldoAntesDeshipotecar - 240,
            $"=> Hipotecada: {propAzul.getEstaHipotecada()}, Saldo anterior: {saldoAntesDeshipotecar}, Saldo actual: {rico.getSaldo()} (-240)");

        // Prueba 4.12: Metodo desHipotecar() con saldo insuficiente
        // Volvemos a hipotecar y dejamos a rico con poco saldo para probar el caso limite
        propAzul.hipotecar();
        rico.setSaldo(50); // Menor que 240
        propAzul.desHipotecar();
        Verificar("4.12 desHipotecar() con saldo insuficiente", 
            propAzul.getEstaHipotecada() == true && rico.getSaldo() == 50,
            $"=> Sigue hipotecada: {propAzul.getEstaHipotecada()}, Saldo sin cambio: {rico.getSaldo()}");

        // Prueba 4.13: Polimorfismo de aplicarCasilla() en Propiedad
        // Con dueño y deshipotecada, aplicarCasilla() debe cobrar alquiler.
        propAzul.setEstaHipotecada(false);
        rico.setSaldo(100);
        rival.setSaldo(500);
        propAzul.aplicarCasilla(rival); // Cobra $50 de alquiler
        Verificar("4.13 aplicarCasilla() polimorfico en Propiedad cobra renta", 
            rival.getSaldo() == 450 && rico.getSaldo() == 150,
            $"=> Inquilino: {rival.getSaldo()} (-50), Duenio: {rico.getSaldo()} (+50)");
        Console.WriteLine();
    }

    // ============================================================================
    // 5. PRUEBAS DE LISTAPROPIEDADES Y NODOPROPIEDAD
    // ============================================================================
    private static void ProbarListaPropiedades()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- [SECCION 5: PRUEBAS DE LISTAPROPIEDADES Y REGLAS] ---");
        Console.ResetColor();

        ListaPropiedades lista = new ListaPropiedades();

        Propiedad pRojo1 = new Propiedad("Calle Serrano", 11, 220, 18, "Rojo", 3);
        Propiedad pRojo2 = new Propiedad("Gran Via", 12, 220, 18, "Rojo", 3);
        Propiedad pRojo3 = new Propiedad("Plaza Mayor", 13, 240, 20, "Rojo", 3);

        Propiedad pAzul1 = new Propiedad("Plaza Espana", 37, 350, 35, "Azul", 2);

        // Prueba 5.1: agregarPropiedad() y calcularValorTotal()
        // 220 + 220 + 240 + 350 = 1030
        lista.agregarPropiedad(pRojo1);
        lista.agregarPropiedad(pRojo2);
        lista.agregarPropiedad(pRojo3);
        lista.agregarPropiedad(pAzul1);

        int totalCalculado = lista.calcularValorTotal();
        Verificar("5.1 agregarPropiedad() y calcularValorTotal()", 
            totalCalculado == 1030,
            $"=> Total calculado: {totalCalculado} (esperado: 1030)");

        // Prueba 5.2: tieneGrupo() cuando el grupo esta completo (Rojo: 3 de 3)
        bool tieneGrupoRojo = lista.tieneGrupo(pRojo1);
        Verificar("5.2 tieneGrupo() con grupo completo (Rojo 3/3)", 
            tieneGrupoRojo == true,
            $"=> Posee grupo completo: {tieneGrupoRojo}");

        // Prueba 5.3: tieneGrupo() cuando el grupo esta incompleto (Azul: 1 de 2)
        bool tieneGrupoAzul = lista.tieneGrupo(pAzul1);
        Verificar("5.3 tieneGrupo() con grupo incompleto (Azul 1/2)", 
            tieneGrupoAzul == false,
            $"=> Posee grupo completo: {tieneGrupoAzul} (esperado: false)");

        // Prueba 5.4: puedeMejorar() con construccion uniforme
        // Inicialmente todas tienen 0 casas. Debe poder construir en pRojo1.
        bool puedeConstruirInicial = lista.puedeMejorar(pRojo1);
        Verificar("5.4 puedeMejorar() con casas empatadas (0 casas en todas)", 
            puedeConstruirInicial == true,
            $"=> Puede mejorar: {puedeConstruirInicial}");

        // Prueba 5.5: puedeMejorar() respetando la regla progresiva
        // Si pRojo1 sube a 1 casa, pRojo2 tiene 0 y pRojo3 tiene 0.
        // pRojo1 NO deberia poder subir a 2 casas porque pRojo2 y pRojo3 tienen menos casas (0 < 1).
        pRojo1.setCasasPuestas(1);
        bool puedeConstruirDesigual = lista.puedeMejorar(pRojo1);
        Verificar("5.5 puedeMejorar() respetando regla progresiva (no subir si otra tiene menos)", 
            puedeConstruirDesigual == false,
            $"=> Puede subir pRojo1 a 2 casas: {puedeConstruirDesigual} (esperado: false)");

        // Prueba 5.6: puedeMejorar() permitiendo nivelar a las demas
        // pRojo2 tiene 0 casas, mientras pRojo1 tiene 1 y pRojo3 tiene 0. pRojo2 SI deberia poder mejorar.
        bool puedeNivelar = lista.puedeMejorar(pRojo2);
        Verificar("5.6 puedeMejorar() para nivelar propiedad atrasada", 
            puedeNivelar == true,
            $"=> Puede subir pRojo2 a 1 casa: {puedeNivelar}");

        // Prueba 5.7: puedeMejorar() cuando ya tiene hotel o 5 casas
        // No debe permitir mejorar mas si ya tiene hotel o 5 casas
        pRojo3.setTieneHotel(true);
        bool mejoraConHotel = lista.puedeMejorar(pRojo3);
        pRojo3.setTieneHotel(false);
        pRojo3.setCasasPuestas(5);
        bool mejoraCon5Casas = lista.puedeMejorar(pRojo3);
        Verificar("5.7 puedeMejorar() limite maximo alcanzado (hotel o 5 casas)", 
            mejoraConHotel == false && mejoraCon5Casas == false,
            $"=> Con Hotel: {mejoraConHotel}, Con 5 Casas: {mejoraCon5Casas}");
        Console.WriteLine();
    }

    // ============================================================================
    // 6. PRUEBAS DEL TABLERO (LISTA CIRCULAR DOBLEMENTE ENLAZADA Y MOVIMIENTO)
    // ============================================================================
    private static void ProbarTablero()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- [SECCION 6: PRUEBAS DEL TABLERO Y MOVIMIENTO] ---");
        Console.ResetColor();

        Tablero tablero = new Tablero();

        // Creamos un mini-tablero circular de 6 casillas para verificar con precision
        // ID 0: Salida (CasillaInicial)
        // ID 1: Carcel (CasillaCarcel)
        // ID 2: Propiedad
        // ID 3: Parqueo Libre (CasillaParqueoLibre)
        // ID 4: Propiedad
        // ID 5: Policia (CasillaPolicia - envia a carcel ID 1)

        CasillaInicial casillaSalida = new CasillaInicial("Salida", 0);
        CasillaCarcel casillaCarcel = new CasillaCarcel("Carcel", 1);
        Propiedad propA = new Propiedad("Avenida 1", 2, 100, 10, "Cafe", 2);
        CasillaParqueoLibre casillaParking = new CasillaParqueoLibre("Parking", 3);
        Propiedad propB = new Propiedad("Avenida 2", 4, 120, 12, "Cafe", 2);
        CasillaPolicia casillaPolicia = new CasillaPolicia("Vaya a la Carcel", 5);

        tablero.agregarCasilla(casillaSalida);
        tablero.agregarCasilla(casillaCarcel);
        tablero.agregarCasilla(propA);
        tablero.agregarCasilla(casillaParking);
        tablero.agregarCasilla(propB);
        tablero.agregarCasilla(casillaPolicia);

        // Prueba 6.1: Verificacion de estructura circular doblemente enlazada
        // Head.anterior debe ser Tail, y Tail.siguiente debe ser Head
        bool circularCorrecto = tablero.getHead().getAnterior() == tablero.getTail() &&
                                tablero.getTail().getSiguiente() == tablero.getHead() &&
                                tablero.getHead().getCasilla().getIdCasilla() == 0 &&
                                tablero.getTail().getCasilla().getIdCasilla() == 5;

        Verificar("6.1 Enlace circular bidireccional de Tablero (Head <-> Tail)", circularCorrecto,
            $"=> Head ID: {tablero.getHead().getCasilla().getIdCasilla()}, Tail ID: {tablero.getTail().getCasilla().getIdCasilla()}");

        // Prueba 6.2: Metodo buscarCasillaPorID()
        NodoCasilla nodoEncontrado = tablero.buscarCasillaPorID(3);
        NodoCasilla nodoNoExistente = tablero.buscarCasillaPorID(99);
        Verificar("6.2 buscarCasillaPorID() existente e inexistente", 
            nodoEncontrado != null && 
            nodoEncontrado.getCasilla().getNombre() == "Parking" && 
            nodoNoExistente == null,
            $"=> Encontrado ID 3: '{nodoEncontrado?.getCasilla().getNombre()}', ID 99: {nodoNoExistente == null}");

        // Prueba 6.3: Metodo moverJugadorACasilla()
        // Mueve directamente al jugador a una casilla por su ID
        Jugador jMov = new Jugador(10, "Camila", tablero.getHead());
        tablero.moverJugadorACasilla(jMov, 4);
        Verificar("6.3 moverJugadorACasilla() directo", 
            jMov.getNodoActual().getCasilla().getIdCasilla() == 4,
            $"=> Casilla actual del jugador: ID {jMov.getNodoActual().getCasilla().getIdCasilla()} ({jMov.getNodoActual().getCasilla().getNombre()})");

        // Prueba 6.4: Metodo moverJugadorPorDados() solo mueve e informa paso por Salida (head)
        // Jugador en ID 4 da 3 pasos en tablero de 6 casillas:
        // Paso 1 -> ID 5 (Policia)
        // Paso 2 -> ID 0 (Salida - Head) => detecta pasoPorSalida = true
        // Paso 3 -> ID 1 (Carcel) => casilla destino final
        int saldoAntesDados = jMov.getSaldo(); // 200
        bool pasoPorSalida = tablero.moverJugadorPorDados(jMov, 3);
        Verificar("6.4 moverJugadorPorDados() detecta paso por Salida", 
            jMov.getNodoActual().getCasilla().getIdCasilla() == 1 &&
            pasoPorSalida == true &&
            jMov.getSaldo() == saldoAntesDados,
            $"=> Casilla final ID: {jMov.getNodoActual().getCasilla().getIdCasilla()}, Paso por salida: {pasoPorSalida}, Saldo: {jMov.getSaldo()}");

        // Prueba 6.5: Aplicar CasillaPolicia y luego teletransportar con fueEncarcelado()
        // Jugador en ID 1 se mueve 4 pasos a ID 5 (CasillaPolicia).
        // Al aplicar la casilla se marca encarcelado y con 3 turnos de sancion.
        tablero.moverJugadorPorDados(jMov, 4);
        jMov.getNodoActual().getCasilla().aplicarCasilla(jMov);
        Verificar("6.5 CasillaPolicia marca encarcelamiento y 3 turnos", 
            jMov.getEstaEncarcelado() == true && 
            jMov.getTurnosPerdidos() == 3 && 
            jMov.getNodoActual().getCasilla().getIdCasilla() == 5,
            $"=> Encarcelado: {jMov.getEstaEncarcelado()}, Turnos: {jMov.getTurnosPerdidos()}, Ubicacion: ID {jMov.getNodoActual().getCasilla().getIdCasilla()}");

        // Prueba 6.6: Metodo fueEncarcelado() en Tablero completo (reubica en ID 10)
        // Creamos un tablero estandar donde existe la casilla 10 para probar la reubicacion
        Tablero tableroCompleto = new Tablero();
        for (int i = 0; i < 40; i++)
        {
            if (i == 10) tableroCompleto.agregarCasilla(new CasillaCarcel("Carcel", 10));
            else tableroCompleto.agregarCasilla(new Casilla("Casilla " + i, i));
        }
        Jugador jPreso = new Jugador(99, "Preso", tableroCompleto.buscarCasillaPorID(30));
        jPreso.setEstaEncarcelado(true);
        tableroCompleto.fueEncarcelado(jPreso);
        Verificar("6.6 fueEncarcelado() reubica al jugador en la carcel (ID 10)", 
            jPreso.getNodoActual().getCasilla().getIdCasilla() == 10,
            $"=> Reubicado en casilla ID: {jPreso.getNodoActual().getCasilla().getIdCasilla()}");

        // Prueba 6.7: Getters y Setters de casas/hoteles restantes del Tablero
        tablero.setCasasRestantes(28);
        tablero.setHotelesRestantes(10);
        Verificar("6.7 Casas y Hoteles restantes del Tablero", 
            tablero.getCasasRestantes() == 28 && tablero.getHotelesRestantes() == 10,
            $"=> Casas restantes: {tablero.getCasasRestantes()}, Hoteles restantes: {tablero.getHotelesRestantes()}");
        Console.WriteLine();
    }

    // ============================================================================
    // 7. PRUEBAS DE LA CLASE BANCO (INTEGRACION Y GESTION DEL JUEGO)
    // ============================================================================
    private static void ProbarBanco()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- [SECCION 7: PRUEBAS DE LA CLASE BANCO] ---");
        Console.ResetColor();

        Banco banco = new Banco();

        // Prueba 7.1: Verificacion de inicializacion de Banco y tablero de 40 casillas
        NodoCasilla head = banco.getTableroJuego().getHead();
        NodoCasilla c10 = banco.getTableroJuego().buscarCasillaPorID(10);
        NodoCasilla c39 = banco.getTableroJuego().buscarCasillaPorID(39);

        bool tableroPoblado = head != null && 
                              head.getCasilla().getIdCasilla() == 0 &&
                              c10 != null && c10.getCasilla().getNombre() == "Carcel" &&
                              c39 != null && c39.getCasilla().getNombre() == "Boardwalk";

        Verificar("7.1 Inicializacion de Banco y 40 casillas", tableroPoblado,
            $"=> Head: {head?.getCasilla().getNombre()} (0), Carcel: {c10?.getCasilla().getNombre()} (10), Ultima: {c39?.getCasilla().getNombre()} (39)");

        // Prueba 7.2: Registrar jugadores en Banco
        Jugador j1 = new Jugador(1, "Jugador 1", null!);
        j1.setSaldo(1500);
        Jugador j2 = new Jugador(2, "Jugador 2", null!);
        j2.setSaldo(1500);

        banco.registrarJugador(j1);
        banco.registrarJugador(j2);

        Verificar("7.2 Registro de jugadores en Banco",
            j1.getNodoActual() == head && banco.getTurnosJugadores().getCantidadJugadores() == 2 && banco.getJugadorActual() == j1,
            $"=> J1 en: {j1.getNodoActual().getCasilla().getNombre()}, Cantidad jugadores: {banco.getTurnosJugadores().getCantidadJugadores()}");

        // Prueba 7.3: ProcesarTirada hacia casilla sin dueño (ID 1 - Mediterraneo)
        // J1 tira dados = 1, llega a ID 1 (Mediterraneo, precio $60).
        // No se debe cobrar nada aun porque no tiene dueño.
        banco.ProcesarTirada(j1, 1);
        Verificar("7.3 ProcesarTirada a propiedad sin duenio",
            j1.getNodoActual().getCasilla().getIdCasilla() == 1 && j1.getSaldo() == 1500,
            $"=> Casilla: {j1.getNodoActual().getCasilla().getNombre()} (ID 1), Saldo: {j1.getSaldo()}");

        // Prueba 7.4: ProcesarCompra de la propiedad actual
        // J1 compra Mediterraneo ($60). Saldo pasa de 1500 a 1440.
        bool compraOk = banco.ProcesarCompra(j1);
        Propiedad med = (Propiedad)j1.getNodoActual().getCasilla();
        Verificar("7.4 ProcesarCompra de propiedad disponible",
            compraOk && med.getDuenio() == j1 && j1.getSaldo() == 1440 && j1.getPropiedades().getSize() == 1,
            $"=> Comprado: {compraOk}, Duenio: {med.getDuenio()?.getNombre()}, Saldo J1: {j1.getSaldo()}");

        // Prueba 7.5: ProcesarFinTurno y ProcesarTirada de J2 cayendo en propiedad de J1
        // Fin de turno pasa a J2. J2 tira 1 y cae en Mediterraneo (alquiler base $2).
        banco.ProcesarFinTurno();
        Verificar("7.5 ProcesarFinTurno avanza turno",
            banco.getJugadorActual() == j2,
            $"=> Turno actual ahora es: {banco.getJugadorActual().getNombre()}");

        banco.ProcesarTirada(j2, 1); // J2 cae en Mediterraneo de J1
        Verificar("7.5.1 Cobro de alquiler via ProcesarTirada",
            j2.getSaldo() == 1500 - med.getAlquiler() && j1.getSaldo() == 1440 + med.getAlquiler(),
            $"=> J2 pago renta: J2 saldo={j2.getSaldo()} (-{med.getAlquiler()}), J1 saldo={j1.getSaldo()} (+{med.getAlquiler()})");

        // Prueba 7.6: ProcesarTirada con paso por Salida (+200)
        // J1 esta en ID 1. Tira 39 dados -> da la vuelta completa y cae en ID 0 (Salida). Cruzo Salida -> +$200.
        int saldoAntesVuelta = j1.getSaldo();
        banco.ProcesarTirada(j1, 39);
        Verificar("7.6 ProcesarTirada con vuelta y cobro de Salida (+$200)",
            j1.getNodoActual().getCasilla().getIdCasilla() == 0 && j1.getSaldo() == saldoAntesVuelta + 200,
            $"=> Posicion J1: {j1.getNodoActual().getCasilla().getNombre()} (ID 0), Saldo: {j1.getSaldo()} (+200)");

        // Prueba 7.7: Caer en Policia (ID 30) manda a Carcel (ID 10)
        // Movemos a J2 a ID 28 y tira 2 -> ID 30 (Vaya a la Carcel).
        banco.getTableroJuego().moverJugadorACasilla(j2, 28);
        banco.ProcesarTirada(j2, 2);
        Verificar("7.7 Caer en Policia encarcela y traslada a Carcel (ID 10)",
            j2.getEstaEncarcelado() == true && j2.getTurnosPerdidos() == 3 && j2.getNodoActual().getCasilla().getIdCasilla() == 10,
            $"=> Encarcelado: {j2.getEstaEncarcelado()}, Turnos: {j2.getTurnosPerdidos()}, Ubicacion: ID {j2.getNodoActual().getCasilla().getIdCasilla()}");

        // Prueba 7.8: Jugador encarcelado cumple sancion y no se mueve
        banco.ProcesarTirada(j2, 5); // Intenta tirar
        Verificar("7.8 Jugador encarcelado reduce turnos perdidos y no avanza",
            j2.getTurnosPerdidos() == 2 && j2.getNodoActual().getCasilla().getIdCasilla() == 10,
            $"=> Turnos restantes: {j2.getTurnosPerdidos()}, Ubicacion intacta: ID {j2.getNodoActual().getCasilla().getIdCasilla()}");

        // Prueba 7.9: Bancarrota y verificacion de ganador unico
        banco.ManejarBancarrota(j2, j1);
        Jugador? ganador = banco.verificarGanador();
        Verificar("7.9 ManejarBancarrota y verificarGanador",
            j2.getEnBancarrota() == true && j2.isActivo() == false && ganador == j1,
            $"=> J2 en bancarrota: {j2.getEnBancarrota()}, Ganador detectado: {ganador?.getNombre()}");

        // Prueba 7.10: TirarDados genera valores validos entre 2 y 12
        int dadosSuma = banco.TirarDados();
        (int d1, int d2) = banco.TirarDadosDetallado();
        Verificar("7.10 TirarDados y TirarDadosDetallado",
            dadosSuma >= 2 && dadosSuma <= 12 && d1 >= 1 && d1 <= 6 && d2 >= 1 && d2 <= 6,
            $"=> Suma: {dadosSuma}, Dado 1: {d1}, Dado 2: {d2}");

        // Prueba 7.11: Construccion de casas (completar grupo Cafe: Mediterraneo ID 1 + Baltico ID 3)
        Propiedad baltico = (Propiedad)banco.getTableroJuego().buscarCasillaPorID(3).getCasilla();
        baltico.comprar(j1); // J1 ahora posee todo el grupo Cafe
        int saldoAntesConstruir = j1.getSaldo();
        int casasStockAntes = banco.getTableroJuego().getCasasRestantes();

        bool construyoCasa = banco.ProcesarConstruirCasa(j1, med);
        Verificar("7.11 ProcesarConstruirCasa con grupo completo",
            construyoCasa && med.getCasasPuestas() == 1 && j1.getSaldo() == saldoAntesConstruir - med.getCostoCasa() && banco.getTableroJuego().getCasasRestantes() == casasStockAntes - 1,
            $"=> Casas en Mediterraneo: {med.getCasasPuestas()}, Saldo J1: {j1.getSaldo()} (-{med.getCostoCasa()}), Stock Banco: {banco.getTableroJuego().getCasasRestantes()}");

        // Prueba 7.12: Venta de casa y reembolso del 50%
        int saldoAntesVender = j1.getSaldo();
        bool vendioCasa = banco.ProcesarVenderCasa(j1, med);
        Verificar("7.12 ProcesarVenderCasa y reembolso 50%",
            vendioCasa && med.getCasasPuestas() == 0 && j1.getSaldo() == saldoAntesVender + (med.getCostoCasa() / 2),
            $"=> Casas restantes: {med.getCasasPuestas()}, Saldo J1: {j1.getSaldo()} (+{med.getCostoCasa() / 2})");

        // Prueba 7.13: Hipotecar y Deshipotecar propiedad
        int saldoAntesHipoteca = j1.getSaldo();
        int valorHipoteca = med.getPrecioDeCompra() / 2; // $60 / 2 = $30
        bool hipotecadaOk = banco.ProcesarHipotecar(j1, med);
        Verificar("7.13.1 ProcesarHipotecar sin casas",
            hipotecadaOk && med.getEstaHipotecada() == true && j1.getSaldo() == saldoAntesHipoteca + valorHipoteca,
            $"=> Hipotecada: {med.getEstaHipotecada()}, Saldo J1: {j1.getSaldo()} (+{valorHipoteca})");

        int costoDeshipoteca = valorHipoteca + (med.getPrecioDeCompra() / 10); // $30 + $6 = $36
        int saldoAntesDeshipoteca = j1.getSaldo();
        bool deshipotecadaOk = banco.ProcesarDeshipotecar(j1, med);
        Verificar("7.13.2 ProcesarDeshipotecar con saldo suficiente",
            deshipotecadaOk && med.getEstaHipotecada() == false && j1.getSaldo() == saldoAntesDeshipoteca - costoDeshipoteca,
            $"=> Deshipotecada: {!med.getEstaHipotecada()}, Saldo J1: {j1.getSaldo()} (-{costoDeshipoteca})");

        // Prueba 7.14: Flujo de bancarrota inminente y resolucion
        Jugador jDeudor = new Jugador(3, "Deudor", banco.getTableroJuego().getHead());
        jDeudor.setSaldo(50);
        jDeudor.marcarBancarrotaInminente(j1, 100);
        Verificar("7.14.1 marcarBancarrotaInminente en Jugador",
            jDeudor.getBancarrotaInminente() == true && jDeudor.getAcreedorPendiente() == j1 && jDeudor.getMontoPendiente() == 100,
            $"=> Inminente: {jDeudor.getBancarrotaInminente()}, Acreedor: {jDeudor.getAcreedorPendiente()?.getNombre()}, Monto: ${jDeudor.getMontoPendiente()}");

        // Intenta resolver sin saldo suficiente (falla)
        bool resolvioSinSaldo = jDeudor.resolverBancarrota();
        // Le damos saldo suficiente y resuelve con exito
        jDeudor.setSaldo(150);
        int saldoJ1AntesResolver = j1.getSaldo();
        bool resolvioConSaldo = jDeudor.resolverBancarrota();
        Verificar("7.14.2 resolverBancarrota tras reunir fondos",
            resolvioSinSaldo == false && resolvioConSaldo == true && jDeudor.getBancarrotaInminente() == false && jDeudor.getSaldo() == 50 && j1.getSaldo() == saldoJ1AntesResolver + 100,
            $"=> Resuelto: {resolvioConSaldo}, Saldo Deudor: ${jDeudor.getSaldo()}, Saldo Acreedor: ${j1.getSaldo()}");

        // Prueba 7.15: Intercambio multiple de propiedades y dinero entre jugadores
        Jugador jTraderA = new Jugador(10, "TraderA", banco.getTableroJuego().getHead());
        jTraderA.setSaldo(1000);
        Jugador jTraderB = new Jugador(20, "TraderB", banco.getTableroJuego().getHead());
        jTraderB.setSaldo(1000);

        Propiedad propA1 = new Propiedad("Calle A1", 101, 100, 10, "ColorA", 2);
        Propiedad propA2 = new Propiedad("Calle A2", 102, 100, 10, "ColorA", 2);
        Propiedad propB1 = new Propiedad("Avenida B1", 201, 200, 20, "ColorB", 2);

        propA1.comprar(jTraderA);
        propA2.comprar(jTraderA);
        propB1.comprar(jTraderB);

        // Preparamos las listas temporales para el trade
        // TraderA ofrece: Calle A1 + Calle A2 + $50
        // TraderB ofrece: Avenida B1 + $0
        ListaPropiedades tradeA = new ListaPropiedades();
        tradeA.agregarPropiedad(propA1);
        tradeA.agregarPropiedad(propA2);

        ListaPropiedades tradeB = new ListaPropiedades();
        tradeB.agregarPropiedad(propB1);

        int saldoA_antes = jTraderA.getSaldo();
        int saldoB_antes = jTraderB.getSaldo();

        bool tradeExitoso = banco.ProcesarIntercambio(jTraderA, tradeA, 50, jTraderB, tradeB, 0);

        bool traspasoOk = propA1.getDuenio() == jTraderB &&
                          propA2.getDuenio() == jTraderB &&
                          propB1.getDuenio() == jTraderA &&
                          jTraderA.getSaldo() == saldoA_antes - 50 &&
                          jTraderB.getSaldo() == saldoB_antes + 50 &&
                          jTraderA.getPropiedades().getSize() == 1 &&
                          jTraderB.getPropiedades().getSize() == 2;

        Verificar("7.15.1 ProcesarIntercambio multiple y dinero",
            tradeExitoso && traspasoOk,
            $"=> Trade Ok: {tradeExitoso}, Duenio A1: {propA1.getDuenio()?.getNombre()}, Duenio B1: {propB1.getDuenio()?.getNombre()}, Saldo A: ${jTraderA.getSaldo()}, Saldo B: ${jTraderB.getSaldo()}");

        // 7.15.2 Rechazar intercambio si una propiedad tiene casas construidas
        propB1.setCasasPuestas(1); // Ponemos casa ficticia
        ListaPropiedades tradeInvalido = new ListaPropiedades();
        tradeInvalido.agregarPropiedad(propB1);
        bool tradeRechazadoPorCasas = banco.ProcesarIntercambio(jTraderA, tradeInvalido, 0, jTraderB, new ListaPropiedades(), 0);
        Verificar("7.15.2 Rechazar intercambio si tiene casas construidas",
            tradeRechazadoPorCasas == false,
            $"=> Rechazado correctamente: {!tradeRechazadoPorCasas}");

        Console.WriteLine();
    }
}



