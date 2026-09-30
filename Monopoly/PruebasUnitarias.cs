using System;
using System.IO;
using MonopolyDistribuido;

namespace Monopoly;

public static class PruebasUnitarias
{
    private static int _pruebasPasadas = 0;
    private static int _pruebasTotales = 0;

    private static void Afirmar(bool condicion, string nombrePrueba)
    {
        _pruebasTotales++;
        if (condicion)
        {
            _pruebasPasadas++;
            Console.WriteLine($"  [PASS] {nombrePrueba}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {nombrePrueba}");
            Console.ResetColor();
            throw new Exception($"Fallo en prueba unitaria: {nombrePrueba}");
        }
    }

    public static void EjecutarTodas()
    {
        Console.WriteLine("\n======================================================================");
        Console.WriteLine("    EJECUTANDO BATERÍA COMPLETA DE PRUEBAS UNITARIAS Y LÓGICA         ");
        Console.WriteLine("======================================================================\n");

        _pruebasPasadas = 0;
        _pruebasTotales = 0;

        TestEstructuraTablero();
        TestEstructuraTurnos();
        TestEstructuraPropiedades();
        TestEstructuraTransacciones();
        TestEstructuraMazoEventos();
        TestPolimorfismoCasillas();
        TestReglasMonopoly();
        TestHardwareFallback();

        Console.WriteLine("\n======================================================================");
        Console.WriteLine($"    RESULTADO: {_pruebasPasadas}/{_pruebasTotales} PRUEBAS PASADAS SATISFACTORIAMENTE (100%)");
        Console.WriteLine("======================================================================\n");
    }

    private static void TestEstructuraTablero()
    {
        Console.WriteLine("[TEST SUITE 1] Estructura de Tablero (Lista Circular Doble)...");
        var tablero = new Tablero();
        tablero.InicializarTablero24();

        Afirmar(tablero.getTotalCasillas() == 24, "Tablero contiene exactamente 24 casillas");
        Afirmar(tablero.getHead() != null, "Head no es nulo");
        Afirmar(tablero.getTail() != null, "Tail no es nulo");

        // Verificar circularidad doble
        Afirmar(tablero.getHead()!.getAnterior() == tablero.getTail(), "Head.Anterior apunta a Tail (doble enlace circular)");
        Afirmar(tablero.getTail()!.getSiguiente() == tablero.getHead(), "Tail.Siguiente apunta a Head (doble enlace circular)");

        // Búsqueda por ID
        var c0 = tablero.buscarCasillaPorID(0);
        Afirmar(c0 != null && c0.getCasilla() is CasillaInicial, "Casilla 0 es CasillaInicial");

        var c6 = tablero.buscarCasillaPorID(6);
        Afirmar(c6 != null && c6.getCasilla() is CasillaCarcel, "Casilla 6 es CasillaCarcel");

        var c18 = tablero.buscarCasillaPorID(18);
        Afirmar(c18 != null && c18.getCasilla() is CasillaPolicia, "Casilla 18 es CasillaPolicia");

        // Movimiento circular y paso por salida
        var jugador = new Jugador(1, "TestPlayer", tablero.getHead(), 1500);
        
        // Mover 5 pasos desde casilla 0 -> debe llegar a casilla 5 sin paso por salida
        var destino1 = tablero.moverJugadorPorDados(jugador, 5, out bool pasoSalida1);
        Afirmar(destino1.getIdCasilla() == 5, "Jugador avanzó a casilla 5");
        Afirmar(!pasoSalida1, "No pasó por la salida en avance 0->5");

        // Mover 20 pasos desde casilla 5 -> total 25 % 24 = casilla 1, debe haber pasado por salida (+₡200)
        int saldoAntes = jugador.getSaldo();
        var destino2 = tablero.moverJugadorPorDados(jugador, 20, out bool pasoSalida2);
        Afirmar(destino2.getIdCasilla() == 1, "Jugador avanzó circularmente a casilla 1 (5 + 20 = 25 -> 1)");
        Afirmar(pasoSalida2, "Detectó cruce por salida (Casilla 0)");
        Afirmar(jugador.getSaldo() == saldoAntes + 200, "Saldo aumentó ₡200 al cruzar la salida");
    }

    private static void TestEstructuraTurnos()
    {
        Console.WriteLine("\n[TEST SUITE 2] Estructura de Turnos (Lista Circular Simple)...");
        var turnos = new ListaTurnos();
        var j1 = new Jugador(1, "J1", null);
        var j2 = new Jugador(2, "J2", null);
        var j3 = new Jugador(3, "J3", null);

        turnos.agregarJugador(j1);
        turnos.agregarJugador(j2);
        turnos.agregarJugador(j3);

        Afirmar(turnos.GetTotalJugadores() == 3, "Total jugadores registrado = 3");
        Afirmar(turnos.getTurnoActual() == j1, "Primer turno es J1");

        // Rotación continua
        var t2 = turnos.avanzarTurno();
        Afirmar(t2 == j2, "Avanzar turno 1 -> J2");
        var t3 = turnos.avanzarTurno();
        Afirmar(t3 == j3, "Avanzar turno 2 -> J3");
        var t1Again = turnos.avanzarTurno();
        Afirmar(t1Again == j1, "Avanzar turno 3 -> J1 (circularidad simple)");

        // Omitir jugadores en bancarrota
        j2.setActivo(false);
        var sigActivo = turnos.avanzarTurno();
        Afirmar(sigActivo == j3, "J2 inactivo fue omitido automáticamente; turno pasó directo a J3");
    }

    private static void TestEstructuraPropiedades()
    {
        Console.WriteLine("\n[TEST SUITE 3] Estructura de Propiedades (Lista Lineal Simple)...");
        var lista = new ListaPropiedades();
        var p1 = new Propiedad("Prop 1", 1, 100, 10, "Rojo");
        var p2 = new Propiedad("Prop 2", 2, 200, 20, "Rojo");

        lista.agregarPropiedad(p1);
        lista.agregarPropiedad(p2);

        Afirmar(lista.getSize() == 2, "Tamaño de lista = 2");
        Afirmar(lista.calcularValorTotal() == 300, "Valor patrimonial total = 100 + 200 = 300");

        var buscada = lista.buscarPorId(2);
        Afirmar(buscada == p2, "Búsqueda por ID devuelve Prop 2");

        bool removida = lista.removerPropiedad(1);
        Afirmar(removida && lista.getSize() == 1, "Prop 1 removida exitosamente; tamaño ahora es 1");
        Afirmar(lista.calcularValorTotal() == 200, "Valor patrimonial recalculado = 200");
    }

    private static void TestEstructuraTransacciones()
    {
        Console.WriteLine("\n[TEST SUITE 4] Estructura de Transacciones y Persistencia...");
        string archivoPrueba = "test_transacciones_unit.txt";
        if (File.Exists(archivoPrueba)) File.Delete(archivoPrueba);

        var historial = new HistorialTransacciones(archivoPrueba);
        var tx = new Transaccion(1, "COMPRA", "Jugador1", "BANCO", 120, "Compra de prueba");
        historial.Registrar(tx);

        Afirmar(historial.GetSize() == 1, "Historial registra 1 transacción en lista");
        Afirmar(File.Exists(archivoPrueba), "Archivo físico generado en disco");

        var lineas = File.ReadAllLines(archivoPrueba);
        Afirmar(lineas.Length >= 2, "Archivo contiene encabezado CSV y línea de transacción");
        Afirmar(lineas[1].Contains("COMPRA") && lineas[1].Contains("120"), "Línea CSV contiene tipo y monto exacto");

        if (File.Exists(archivoPrueba)) File.Delete(archivoPrueba);
    }

    private static void TestEstructuraMazoEventos()
    {
        Console.WriteLine("\n[TEST SUITE 5] Estructura de Mazo de Eventos (Cola Circular)...");
        var mazo = new MazoEventos();

        var c1 = mazo.TomarCarta();
        var c2 = mazo.TomarCarta();
        Afirmar(c1 != null && c2 != null, "Cartas tomadas no son nulas");
        Afirmar(c1!.Id != c2!.Id, "Cartas sucesivas son distintas (rotación en cola circular)");
    }

    private static void TestPolimorfismoCasillas()
    {
        Console.WriteLine("\n[TEST SUITE 6] Polimorfismo de Casillas...");
        var tablero = new Tablero();
        tablero.InicializarTablero24();

        var jDueno = new Jugador(10, "Duenio", null, 1000);
        var jInquilino = new Jugador(20, "Inquilino", null, 1000);

        var prop = new Propiedad("Avenida Test", 5, 200, 50, "Azul");
        
        // 1. Casilla sin dueño -> Disponible para compra
        string res1 = prop.aplicarCasilla(jInquilino, tablero);
        Afirmar(res1.StartsWith("PROPIEDAD_DISPONIBLE|"), "Propiedad sin dueño devuelve PROPIEDAD_DISPONIBLE");

        // 2. Compra de propiedad
        bool compro = prop.comprar(jDueno);
        Afirmar(compro, "Dueño compró la propiedad");
        Afirmar(jDueno.getSaldo() == 800, "Saldo de dueño descontado (1000 - 200 = 800)");
        Afirmar(prop.getDuenio() == jDueno, "Dueño asignado correctamente");

        // 3. Inquilino cae en propiedad ajena -> Cobro de alquiler
        string res2 = prop.aplicarCasilla(jInquilino, tablero);
        Afirmar(jInquilino.getSaldo() == 950, "Inquilino pagó alquiler (1000 - 50 = 950)");
        Afirmar(jDueno.getSaldo() == 850, "Dueño recibió alquiler (800 + 50 = 850)");

        // 4. Casilla Policía -> envía a la cárcel
        var policia = new CasillaPolicia("Policia", 18);
        policia.aplicarCasilla(jInquilino, tablero);
        Afirmar(jInquilino.isEncarcelado(), "Jugador queda encarcelado");
        Afirmar(jInquilino.getNodoActual()?.getCasilla().getIdCasilla() == 6, "Jugador fue trasladado a la Casilla 6 (Cárcel)");
    }

    private static void TestReglasMonopoly()
    {
        Console.WriteLine("\n[TEST SUITE 7] Validaciones de Reglas de Juego...");
        var prop = new Propiedad("Solar Test", 1, 300, 40, "Verde");
        var pobre = new Jugador(99, "Pobre", null, 100);

        // No comprar sin saldo suficiente
        bool intentoCompra = prop.comprar(pobre);
        Afirmar(!intentoCompra, "No permite comprar sin saldo suficiente (₡100 < ₡300)");
        Afirmar(!prop.tieneDuenio(), "Propiedad sigue sin dueño");

        // Hipoteca: otorga el 50% del valor de compra
        var rico = new Jugador(1, "Rico", null, 500);
        prop.comprar(rico); // paga 300, queda en 200
        bool hipotecada = prop.hipotecar();
        Afirmar(hipotecada, "Propiedad hipotecada con éxito");
        Afirmar(rico.getSaldo() == 200 + 150, "Dueño recibió el 50% del valor de compra (₡150)");

        // No cobra alquiler mientras esté hipotecada
        var visitante = new Jugador(2, "Visitante", null, 500);
        prop.aplicarCasilla(visitante, new Tablero());
        Afirmar(visitante.getSaldo() == 500, "Visitante no paga alquiler porque la propiedad está hipotecada");

        // Deshipotecar: paga 50% + 10% = 150 + 30 = 180
        bool deshipotecada = prop.desHipotecar();
        Afirmar(deshipotecada, "Deshipotecada con éxito");
        Afirmar(rico.getSaldo() == 350 - 180, "Saldo de dueño descontó 50% + 10% (₡180)");
    }

    private static void TestHardwareFallback()
    {
        Console.WriteLine("\n[TEST SUITE 8] Tolerancia a Fallos de Hardware y Display...");
        var hw = new ControladorHardware(forzarSimulado: true);

        // RFID retorna null inmediatamente sin bloquear
        string? rfid = hw.SolicitarRfid(500);
        Afirmar(rfid == null, "SolicitarRfid sin hardware retorna null inmediatamente (fallback)");

        // 7 segmentos formatea y no lanza excepciones
        bool display = hw.MostrarEnDisplay(88);
        Afirmar(!display, "Display retorna false sin excepción cuando no hay puerto físico");

        // Tirada de dados funciona y genera valores 2 a 12
        var (d1, d2, tot) = hw.TirarDados();
        Afirmar(d1 >= 1 && d1 <= 6 && d2 >= 1 && d2 <= 6 && tot == d1 + d2, $"Dados generados en rango válido: {d1} + {d2} = {tot}");
    }
}
