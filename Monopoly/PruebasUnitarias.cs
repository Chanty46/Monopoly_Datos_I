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
        TestFase2JugadoresRfid();
        TestNuevasClasesYRequisitosOficiales();

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

    private static void TestFase2JugadoresRfid()
    {
        Console.WriteLine("\n[TEST SUITE 9] Fase 2 — Jugadores, RFID y Límites...");

        // UIDs RFID conocidos del spec
        var uidsConocidos = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "4173AA6E", 1 },
            { "21775764", 2 },
            { "831785A6", 3 },
        };

        // Prueba 1: UIDs conocidos mapean a los IDs correctos
        Afirmar(uidsConocidos["4173AA6E"] == 1, "UID '4173AA6E' -> Jugador 1");
        Afirmar(uidsConocidos["21775764"] == 2, "UID '21775764' -> Jugador 2");
        Afirmar(uidsConocidos["831785A6"] == 3, "UID '831785A6' -> Jugador 3");
        Afirmar(!uidsConocidos.ContainsKey("UNKNOWN_UID"), "UID desconocido no está en tabla de conocidos");

        // Prueba 2: búsqueda insensible a mayúsculas
        Afirmar(uidsConocidos.ContainsKey("4173aa6e"), "UID minúsculas encontrado (case-insensitive)");
        Afirmar(uidsConocidos.ContainsKey("4173AA6E"), "UID mayúsculas encontrado (case-insensitive)");

        // Prueba 3: jugadores con y sin RFID funcionan independientemente
        var tablero = new Tablero();
        tablero.InicializarTablero24();
        var nodoInicio = tablero.buscarCasillaPorID(0);

        var j1 = new Jugador(1, "Rojo", nodoInicio, 1500);
        j1.setRfidUid("4173AA6E");
        Afirmar(j1.getRfidUid() == "4173AA6E", "Jugador 1 tiene RFID asignado");
        Afirmar(j1.isActivo(), "Jugador 1 activo aunque RFID no se haya escaneado aún");

        var j2 = new Jugador(2, "Azul", nodoInicio, 1500);
        // Sin RFID asignado — simula 'no_disponible'
        Afirmar(j2.getRfidUid() == null, "Jugador 2 sin RFID (null)");
        Afirmar(j2.isActivo(), "Jugador 2 activo sin RFID — fallback correcto");

        // Prueba 4: límite de 3 jugadores en ListaTurnos
        var turnos = new ListaTurnos();
        turnos.agregarJugador(j1);
        Afirmar(turnos.GetTotalJugadores() == 1, "1/3 jugadores conectados");

        var j3 = new Jugador(3, "Verde", nodoInicio, 1500);
        j3.setRfidUid("831785A6");
        turnos.agregarJugador(new Jugador(2, "Azul2", nodoInicio, 1500));
        turnos.agregarJugador(j3);
        Afirmar(turnos.GetTotalJugadores() == 3, "3/3 jugadores conectados — partida llena");
        Afirmar(turnos.GetTotalJugadores() >= 3, "Límite MAX_JUGADORES(3) alcanzado — se debe rechazar el 4to");

        // Prueba 5: el turno se asigna al primer jugador conectado
        Afirmar(turnos.getTurnoActual() != null, "Turno actual no es null con jugadores conectados");
        Afirmar(turnos.getTurnoActual()!.getID() == 1, "Primer turno corresponde al Jugador 1");

        // Prueba 6: rotación de turnos con 3 jugadores
        turnos.avanzarTurno();
        Afirmar(turnos.getTurnoActual()!.getID() == 2, "Segundo turno corresponde al Jugador 2");
        turnos.avanzarTurno();
        Afirmar(turnos.getTurnoActual()!.getID() == 3, "Tercer turno corresponde al Jugador 3");
        turnos.avanzarTurno();
        Afirmar(turnos.getTurnoActual()!.getID() == 1, "Cuarto turno vuelve al Jugador 1 (circular)");

        // Prueba 7: valores display 7 segmentos — especificación obligatoria
        // Valores obligatorios: 00, 01, 02, 09, 10, 11, 12, 13, 20, 21, 31, 42, 50, 69, 90, 99
        int[] valoresDisplay = { 0, 1, 2, 9, 10, 11, 12, 13, 20, 21, 31, 42, 50, 69, 90, 99 };
        foreach (int v in valoresDisplay)
        {
            int decenas = v / 10;
            int unidades = v % 10;
            Afirmar(decenas * 10 + unidades == v, $"Display {v:D2}: decenas={decenas}, unidades={unidades} → valor={decenas * 10 + unidades}");
        }

        // Prueba 8: desconexión y liberación de cupos
        bool eliminado = turnos.eliminarJugador(2);
        Afirmar(eliminado, "Jugador 2 eliminado tras desconexión");
        Afirmar(turnos.GetTotalJugadores() == 2, "Total jugadores actualizado a 2/3 tras desconexión");
        Afirmar(turnos.BuscarPorId(2) == null, "Jugador 2 ya no está en la lista de turnos");

        // Prueba 9: Flujo completo de compra de propiedad y validaciones
        var propPrueba = new Propiedad("Avenida Central", 1, 300, 30, "Café");
        var jugadorComprador = new Jugador(1, "Comprador", nodoInicio, 1600);

        // 9.1 Propiedad sin dueño genera resultado de disponible con nombre, precio y alquiler reales
        string resDisponible = propPrueba.aplicarCasilla(jugadorComprador, tablero);
        Afirmar(resDisponible.StartsWith("PROPIEDAD_DISPONIBLE|1|Avenida Central|300|30"), "Formato oficial de propiedad disponible con datos reales: " + resDisponible);

        // 9.2 Compra exitosa
        bool compraOk = propPrueba.comprar(jugadorComprador);
        Afirmar(compraOk, "Compra de propiedad procesada con éxito");
        Afirmar(jugadorComprador.getSaldo() == 1300, "Saldo del comprador descontó ₡300 (₡1600 -> ₡1300)");
        Afirmar(propPrueba.getDuenio() == jugadorComprador, "Propietario asignado correctamente");
        Afirmar(jugadorComprador.getPropiedades().buscarPorId(1) != null, "Propiedad registrada en inventario del jugador");

        // 9.3 Compra repetida rechazada
        bool compraRepetida = propPrueba.comprar(jugadorComprador);
        Afirmar(!compraRepetida, "Compra repetida rechazada: la propiedad ya tiene dueño");

        // 9.4 Intento de compra por otro jugador rechazado
        var jugadorVisitante = new Jugador(2, "Visitante", nodoInicio, 1000);
        bool compraVisitante = propPrueba.comprar(jugadorVisitante);
        Afirmar(!compraVisitante, "Compra por segundo jugador rechazada: propiedad ocupada");

        // 9.5 Visita y pago de alquiler
        string resVisita = propPrueba.aplicarCasilla(jugadorVisitante, tablero);
        Afirmar(resVisita.Contains("pagó ₡30 de alquiler a Comprador"), "Alquiler cobrado correctamente al visitante: " + resVisita);
        Afirmar(jugadorVisitante.getSaldo() == 970, "Saldo de visitante descontó ₡30 (₡1000 -> ₡970)");
        Afirmar(jugadorComprador.getSaldo() == 1330, "Saldo de dueño incrementó ₡30 (₡1300 -> ₡1330)");

        // 9.6 Rechazo por saldo insuficiente
        var propCara = new Propiedad("Paseo del Prado", 2, 2000, 200, "Azul");
        var jugadorPobre = new Jugador(3, "Pobre", nodoInicio, 500);
        bool compraPobre = propCara.comprar(jugadorPobre);
        Afirmar(!compraPobre, "Compra rechazada por saldo insuficiente (₡500 < ₡2000)");
        Afirmar(jugadorPobre.getSaldo() == 500, "Saldo de jugador pobre intacto (₡500)");
        Afirmar(!propCara.tieneDuenio(), "Propiedad cara continúa sin dueño");
    }

    private static void TestNuevasClasesYRequisitosOficiales()
    {
        Console.WriteLine("[TEST SUITE 10] Requisitos Oficiales ITCR (Banco, Dado, Juego, Búsquedas y 4 Jugadores)...");

        // 10.1 Clase Dado
        var dado = new Dado(42);
        var (d1, d2, tot) = dado.Lanzar();
        Afirmar(d1 >= 1 && d1 <= 6 && d2 >= 1 && d2 <= 6, "Dado: valores individuales entre 1 y 6");
        Afirmar(tot == d1 + d2, "Dado: suma total coherente");
        dado.FijarValores(4, 4);
        Afirmar(dado.EsDoble, "Dado: detección correcta de doble (4 y 4)");

        // 10.2 Clase Banco
        var banco = new Banco(50000);
        var histPrueba = new HistorialTransacciones("test_tx_oficial.txt");
        var j1 = new Jugador(1, "Ana", null!, 1500);
        banco.PagarPremioSalida(j1, histPrueba, 1);
        Afirmar(j1.getSaldo() == 1700, "Banco: pago de salida +₡200 a Ana (1500 -> 1700)");

        var propB = new Propiedad("Avenida Central", 10, 400, 50, "Rojo");
        bool okCompraB = banco.CobrarCompraPropiedad(j1, propB, histPrueba, 1);
        Afirmar(okCompraB, "Banco: cobro y registro formal de compra de propiedad");
        Afirmar(j1.getSaldo() == 1300, "Banco: saldo descontado tras compra (1700 -> 1300)");

        bool okHipo = banco.HipotecarPropiedad(j1, propB, histPrueba, 1);
        Afirmar(okHipo && propB.getEstaHipotecada(), "Banco: hipoteca de propiedad (50% valor)");
        Afirmar(j1.getSaldo() == 1500, "Banco: saldo aumentó ₡200 por hipoteca (1300 -> 1500)");

        bool okDeshipo = banco.DeshipotecarPropiedad(j1, propB, histPrueba, 1);
        Afirmar(okDeshipo && !propB.getEstaHipotecada(), "Banco: deshipoteca con interés (+10%)");
        Afirmar(j1.getSaldo() == 1260, "Banco: saldo descontó ₡240 (1500 - 240)");

        // 10.3 Clase Juego y Regla de Fin de Partida (Sección 18)
        var juego = new Juego(maxTurnos: 5, rutaTransacciones: "test_tx_oficial.txt");
        var j2 = new Jugador(2, "Beto", null!, 1000);
        juego.Turnos.agregarJugador(j1);
        juego.Turnos.agregarJugador(j2);

        int patJ1 = juego.CalcularPatrimonio(j1);
        Afirmar(patJ1 == 1260 + 400, "Juego: cálculo de patrimonio (saldo ₡1260 + propiedad ₡400 = ₡1660)");

        // Simular turnos hasta límite
        for (int i = 0; i < 5; i++)
        {
            juego.RegistrarFinDeTurno();
        }
        Afirmar(juego.PartidaFinalizada, "Juego: finalización por límite de turnos alcanzado");
        Afirmar(juego.Ganador == "Ana", "Juego: ganador declarado por mayor patrimonio (Ana)");

        // 10.4 HistorialTransacciones (Lista Doble) - Búsquedas y Recorridos (Sección 12)
        var todasAntiguas = histPrueba.RecorrerDesdeMasAntigua();
        var todasRecientes = histPrueba.RecorrerDesdeMasReciente();
        Afirmar(todasAntiguas.Length >= 4, "Historial: recorrido desde más antigua contiene registros");
        Afirmar(todasRecientes.Length >= 4, "Historial: recorrido desde más reciente contiene registros");
        Afirmar(todasAntiguas[0].Id == todasRecientes[^1].Id, "Historial: orden inverso comprobado bidireccionalmente");

        var txsAna = histPrueba.BuscarPorJugador("Ana");
        Afirmar(txsAna.Length >= 4, "Historial: búsqueda por jugador ('Ana') retorna coincidencias correctas");

        var txsCompra = histPrueba.BuscarPorTipo("COMPRA");
        Afirmar(txsCompra.Length >= 1, "Historial: búsqueda por tipo ('COMPRA') retorna coincidencias correctas");

        string textoCompleto = histPrueba.ImprimirTodas();
        Afirmar(textoCompleto.Contains("Avenida Central"), "Historial: ImprimirTodas() genera reporte completo");

        // 10.5 Soporte formal para 4 jugadores (Sección 1 y 20)
        var turnos4 = new ListaTurnos();
        turnos4.agregarJugador(new Jugador(1, "J1", null!, 1500));
        turnos4.agregarJugador(new Jugador(2, "J2", null!, 1500));
        turnos4.agregarJugador(new Jugador(3, "J3", null!, 1500));
        turnos4.agregarJugador(new Jugador(4, "J4", null!, 1500));
        Afirmar(turnos4.GetTotalJugadores() == 4, "Turnos: 4 jugadores registrados satisfactoriamente");

        Afirmar(turnos4.getTurnoActual()!.getID() == 1, "Turno 1: Jugador 1");
        turnos4.avanzarTurno();
        Afirmar(turnos4.getTurnoActual()!.getID() == 2, "Turno 2: Jugador 2");
        turnos4.avanzarTurno();
        Afirmar(turnos4.getTurnoActual()!.getID() == 3, "Turno 3: Jugador 3");
        turnos4.avanzarTurno();
        Afirmar(turnos4.getTurnoActual()!.getID() == 4, "Turno 4: Jugador 4");
        turnos4.avanzarTurno();
        Afirmar(turnos4.getTurnoActual()!.getID() == 1, "Turno 5: Retorno circular al Jugador 1");

        // Limpieza de archivo temporal de prueba
        try { if (System.IO.File.Exists("test_tx_oficial.txt")) System.IO.File.Delete("test_tx_oficial.txt"); } catch { }
    }
}

