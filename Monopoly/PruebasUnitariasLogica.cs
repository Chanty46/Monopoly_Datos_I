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
        CasillaEvento evento = new CasillaEvento("Suerte", 7);
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

        // Prueba 6.4: Metodo moverJugadorPorDados() avance circular y paso por Salida (+200)
        // Jugador en ID 4 da 3 pasos en tablero de 6 casillas:
        // Paso 1 -> ID 5 (Policia)
        // Paso 2 -> ID 0 (Salida - Head) => cobra $200
        // Paso 3 -> ID 1 (Carcel) => casilla destino final (como visita)
        int saldoAntesDados = jMov.getSaldo(); // 200
        tablero.moverJugadorPorDados(jMov, 3);
        Verificar("6.4 moverJugadorPorDados() con paso por Salida", 
            jMov.getNodoActual().getCasilla().getIdCasilla() == 1 &&
            jMov.getSaldo() == saldoAntesDados + 200,
            $"=> Casilla final ID: {jMov.getNodoActual().getCasilla().getIdCasilla()}, Saldo: {jMov.getSaldo()} (+$200 por pasar por salida)");

        // Prueba 6.5: Metodo moverJugadorPorDados() al caer en CasillaPolicia
        // Jugador en ID 1 da 4 pasos:
        // ID 1 -> ID 2 -> ID 3 -> ID 4 -> ID 5 (CasillaPolicia)
        // CasillaPolicia activa encarcelado=true, turnos=3.
        // moverJugadorPorDados() llama a fueEncarcelado(), que lo teletransporta inmediatamente a la Carcel (ID 1).
        tablero.moverJugadorPorDados(jMov, 4);
        Verificar("6.5 moverJugadorPorDados() cayendo en Casilla Policia (va a carcel ID 1)", 
            jMov.getEstaEncarcelado() == true && 
            jMov.getTurnosPerdidos() == 3 && 
            jMov.getNodoActual().getCasilla().getIdCasilla() == 1,
            $"=> Encarcelado: {jMov.getEstaEncarcelado()}, Turnos: {jMov.getTurnosPerdidos()}, Ubicacion final: ID {jMov.getNodoActual().getCasilla().getIdCasilla()}");

        // Prueba 6.6: Metodo fueEncarcelado() directamente
        // Si el jugador esta marcado encarcelado pero no esta en la carcel, fueEncarcelado() lo envia al ID 1
        jMov.setNodoActual(tablero.buscarCasillaPorID(2)); // Lo forzamos a estar en ID 2
        tablero.fueEncarcelado(jMov);
        Verificar("6.6 fueEncarcelado() reubica al jugador en la carcel (ID 1)", 
            jMov.getNodoActual().getCasilla().getIdCasilla() == 1,
            $"=> Reubicado en casilla ID: {jMov.getNodoActual().getCasilla().getIdCasilla()}");

        // Prueba 6.7: Getters y Setters de casas/hoteles restantes del Tablero
        tablero.setCasasRestantes(28);
        tablero.setHotelesRestantes(10);
        Verificar("6.7 Casas y Hoteles restantes del Tablero", 
            tablero.getCasasRestantes() == 28 && tablero.getHotelesRestantes() == 10,
            $"=> Casas restantes: {tablero.getCasasRestantes()}, Hoteles restantes: {tablero.getHotelesRestantes()}");
        Console.WriteLine();
    }
}
