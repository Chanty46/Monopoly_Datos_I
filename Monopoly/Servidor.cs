using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Monopoly;

namespace MonopolyDistribuido;

public class Servidor
{
    private const int MAX_JUGADORES = 4;

    // Tabla de UIDs RFID conocidos → ID de jugador (1-based)
    // El ID de jugador se asigna inmediatamente al conectar; RFID solo enriquece
    // el registro una vez detectado. La ausencia de RFID NUNCA bloquea al jugador.
    private static readonly Dictionary<string, int> RFID_CONOCIDOS = new(StringComparer.OrdinalIgnoreCase)
    {
        { "4173AA6E", 1 },  // Ficha Roja - Jugador 1
        { "21775764", 2 },  // Ficha Azul - Jugador 2
        { "831785A6", 3 },  // Ficha Verde - Jugador 3
        { "F5A198B2", 4 },  // Ficha Amarilla - Jugador 4
    };

    private readonly TcpListener _listener;
    private readonly ControladorHardware _hw;
    private readonly Juego _juego;
    private readonly Tablero _tablero;
    private readonly ListaTurnos _turnos;
    private readonly HistorialTransacciones _historial;
    private readonly Banco _banco;
    private readonly Dado _dado;
    private readonly Dictionary<TcpClient, Jugador> _clientes = new();
    private readonly object _lockJuego = new();
    private bool _corriendo = true;
    private int _siguienteIdJugador = 1;

    // Estado del turno activo y visualización
    private bool _dadosLanzadosEnTurnoActual = false;
    private Propiedad? _propiedadPendienteCompra = null;
    private ServidorWeb? _servidorWeb;
    private int _ultimoDado1 = 0;
    private int _ultimoDado2 = 0;
    private int _ultimoTotalDados = 0;
    private int _ultimoValorDisplay = 0;

    // Estado RFID por jugador: "esperando" | "identificado" | "no_disponible"
    private readonly Dictionary<int, string> _rfidEstado = new();

    public Juego GetJuego() => _juego;
    public Banco GetBanco() => _banco;

    public Servidor(int puertoTcp, ControladorHardware hardware)
    {
        _hw = hardware;
        _juego = new Juego(maxTurnos: 100, rutaTransacciones: "transacciones.txt");
        _tablero = _juego.Tablero;
        _tablero.InicializarTablero24();
        _turnos = _juego.Turnos;
        _historial = _juego.Historial;
        _banco = _juego.Banco;
        _dado = _juego.Dado;
        _listener = new TcpListener(IPAddress.Any, puertoTcp);
    }

    public void Iniciar()
    {
        try
        {
            _listener.Start();
            Console.WriteLine("=================================================");
            Console.WriteLine("[SERVIDOR] Monopoly Distribuido iniciado.");
            Console.WriteLine($"[SERVIDOR] Escuchando conexiones TCP en puerto {((IPEndPoint)_listener.LocalEndpoint).Port}...");
            Console.WriteLine($"[SERVIDOR] Estado de hardware Raspberry: {_hw.Estado} (Puerto: {_hw.PuertoActual})");
            Console.WriteLine("=================================================");

            // Iniciar servidor web embebido para la interfaz gráfica en puerto 8080
            _servidorWeb = new ServidorWeb(this, puerto: 8080);
            _servidorWeb.Iniciar();

            // Iniciar hilo de escaneo no bloqueante de RFID para jugadores en espera
            IniciarEscaneoRfidSegundoPlano();

            new Thread(AceptarClientes) { IsBackground = true, Name = "AceptarClientesThread" }.Start();

            // Menú interactivo en la consola del Servidor
            while (_corriendo)
            {
                Console.WriteLine("\n[MENU SERVIDOR]");
                Console.WriteLine("  1. Ver estado del juego y jugadores");
                Console.WriteLine("  2. Ver historial de transacciones");
                Console.WriteLine("  3. Probar lectura RFID");
                Console.WriteLine("  4. Probar Display 7 segmentos");
                Console.WriteLine("  5. Salir");
                Console.Write("Servidor> ");

                var op = Console.ReadLine();
                if (op == "1") MostrarEstadoJuegoEnServidor();
                else if (op == "2") Console.WriteLine(_historial.ObtenerHistorialTexto(15));
                else if (op == "3") ProbarRfidConsola();
                else if (op == "4") ProbarDisplayConsola();
                else if (op == "5") { _corriendo = false; _listener.Stop(); _servidorWeb.Detener(); _hw.Detener(); break; }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVIDOR ERROR FATAL] {ex.Message}");
        }
    }

    private void AceptarClientes()
    {
        while (_corriendo)
        {
            try
            {
                var socket = _listener.AcceptTcpClient();
                Console.WriteLine($"[SERVIDOR] Nueva conexión TCP entrante desde {socket.Client.RemoteEndPoint}.");
                new Thread(() => AtenderCliente(socket)) { IsBackground = true }.Start();
            }
            catch
            {
                break;
            }
        }
    }

    private void AtenderCliente(TcpClient client)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        Jugador? jugadorDeEsteCliente = null;

        try
        {
            while (_corriendo && client.Connected)
            {
                var linea = reader.ReadLine();
                if (linea == null) break;

                Console.WriteLine($"[REQ <- {jugadorDeEsteCliente?.getNombre() ?? "Anonimo"}]: {linea}");
                ProcesarComando(client, writer, ref jugadorDeEsteCliente, linea.Trim());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVIDOR INFO] Conexión cerrada con cliente: {ex.Message}");
        }
        finally
        {
            ManejarDesconexionCliente(client, jugadorDeEsteCliente);
        }
    }

    private void ProcesarComando(TcpClient client, StreamWriter writer, ref Jugador? jugador, string comandoLinea)
    {
        if (string.IsNullOrWhiteSpace(comandoLinea)) return;

        var partes = comandoLinea.Split('|');
        string accion = partes[0].ToUpperInvariant();

        lock (_lockJuego)
        {
            switch (accion)
            {
                case "CONECTAR":
                    ManejarConectar(client, writer, ref jugador, partes);
                    break;

                case "TIRAR_DADOS":
                    ManejarTirarDados(writer, jugador);
                    break;

                case "COMPRAR_PROPIEDAD":
                    ManejarComprarPropiedad(writer, jugador);
                    break;

                case "NO_COMPRAR":
                    ManejarNoComprar(writer, jugador);
                    break;

                case "TERMINAR_TURNO":
                    ManejarTerminarTurno(writer, jugador);
                    break;

                case "CONSULTAR_ESTADO":
                    writer.WriteLine($"ESTADO|{GenerarResumenEstado()}");
                    break;

                case "CONSULTAR_TRANSACCIONES":
                    writer.WriteLine($"TRANSACCIONES|{_historial.ObtenerHistorialTexto(10)}");
                    break;

                case "HIPOTECAR":
                    ManejarHipotecar(writer, jugador, partes);
                    break;

                case "DESHIPOTECAR":
                    ManejarDeshipotecar(writer, jugador, partes);
                    break;

                default:
                    writer.WriteLine("ERROR|Comando desconocido.");
                    break;
            }
        }
    }

    private void ManejarConectar(TcpClient client, StreamWriter writer, ref Jugador? jugador, string[] partes)
    {
        // Límite estricto: máximo MAX_JUGADORES (3) jugadores en la partida
        if (_turnos.GetTotalJugadores() >= MAX_JUGADORES)
        {
            writer.WriteLine($"ERROR_LIMITE_JUGADORES|La partida ya tiene {MAX_JUGADORES} jugadores registrados. No se aceptan más conexiones.");
            Console.WriteLine($"[SERVER] Conexión rechazada — ya hay {MAX_JUGADORES}/{MAX_JUGADORES} jugadores.");
            return;
        }

        string nombre = partes.Length > 1 && !string.IsNullOrWhiteSpace(partes[1]) ? partes[1].Trim() : $"Jugador_{_siguienteIdJugador}";
        
        var nodoInicio = _tablero.buscarCasillaPorID(0); // Salida
        int idAsignado = _siguienteIdJugador++;
        jugador = new Jugador(idAsignado, nombre, nodoInicio, 1500);

        // 1. Si el cliente envió un UID en el mensaje de conexión, usarlo
        if (partes.Length > 2 && !string.IsNullOrWhiteSpace(partes[2]))
        {
            string uid = partes[2].Trim().ToUpperInvariant();
            jugador.setRfidUid(uid);
            _rfidEstado[idAsignado] = "identificado";
        }
        else
        {
            // 2. Buscar si este ID de jugador tiene un UID RFID conocido pre-asignado
            string? uidConocido = null;
            foreach (var kvp in RFID_CONOCIDOS)
            {
                if (kvp.Value == idAsignado)
                {
                    uidConocido = kvp.Key;
                    break;
                }
            }

            if (uidConocido != null)
            {
                // Tiene UID conocido registrado — se actualizará cuando el RFID responda.
                // El jugador puede jugar ahora mismo SIN necesitar el RFID.
                jugador.setRfidUid(uidConocido);
                _rfidEstado[idAsignado] = "esperando"; // RFID conocido, pendiente de escanear
            }
            else
            {
                _rfidEstado[idAsignado] = "no_disponible"; // Sin RFID asignado para este puesto
            }
        }

        _turnos.agregarJugador(jugador);
        _clientes[client] = jugador;

        _historial.Registrar(new Transaccion(
            _turnos.GetNumeroRonda(),
            "CONEXION",
            "SISTEMA",
            jugador.getNombre(),
            1500,
            $"Jugador {jugador.getNombre()} (ID {jugador.getID()}) ingresó a la partida con ₡1500."
        ));

        Console.WriteLine($"[SERVER] Jugador registrado: {jugador.getNombre()} (ID: {jugador.getID()}) — RFID: {_rfidEstado[idAsignado]} — [{_turnos.GetTotalJugadores()}/{MAX_JUGADORES}]");

        // Informar al cliente que se unió (incluye conteo X/3)
        writer.WriteLine($"BIENVENIDO|{jugador.getID()}|{jugador.getNombre()}|{jugador.getSaldo()}|{_tablero.getTotalCasillas()}|{_turnos.getTurnoActual()?.getID()}|{_turnos.GetTotalJugadores()}/{MAX_JUGADORES}");

        // Actualizar el display con el jugador actual en turno
        if (_turnos.getTurnoActual() != null)
        {
            _hw.MostrarEnDisplay(_turnos.getTurnoActual()!.getID());
        }

        // Difundir a todos los clientes
        TransmitirATodos($"ACTUALIZACION|Nuevo jugador conectado: {jugador.getNombre()} (Saldo: ₡{jugador.getSaldo()}). Jugadores: {_turnos.GetTotalJugadores()}/{MAX_JUGADORES}. Turno actual: {_turnos.getTurnoActual()?.getNombre()}");
    }

    private void ManejarTirarDados(StreamWriter writer, Jugador? jugador)
    {
        if (jugador == null)
        {
            writer.WriteLine("ERROR|Debe registrarse primero con CONECTAR|<nombre>.");
            return;
        }

        var turnoActual = _turnos.getTurnoActual();
        if (turnoActual == null || turnoActual.getID() != jugador.getID())
        {
            writer.WriteLine($"ERROR|No es su turno. El turno es de {turnoActual?.getNombre() ?? "Nadie"}.");
            return;
        }

        if (_dadosLanzadosEnTurnoActual)
        {
            writer.WriteLine("ERROR|Ya lanzó los dados en este turno. Debe terminar su turno o realizar una acción de propiedad.");
            return;
        }

        // Verificar si está en la cárcel
        if (jugador.isEncarcelado())
        {
            jugador.setTurnosPerdidos(jugador.getTurnosPerdidos() - 1);
            _dadosLanzadosEnTurnoActual = true;
            if (jugador.getTurnosPerdidos() > 0)
            {
                writer.WriteLine($"INFO|Está en la cárcel. Le restan {jugador.getTurnosPerdidos()} turnos.");
                TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} pasa su turno en la cárcel (restan {jugador.getTurnosPerdidos()}).");
                return;
            }
            else
            {
                TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} cumplió su tiempo en la cárcel y queda libre.");
            }
        }

        // =====================================================================
        // PASO OBLIGATORIO: INTENTO DE IDENTIFICACIÓN RFID CON FALLBACK SEGURO
        // =====================================================================
        Console.WriteLine($"[SERVER] Verificando jugador para tirada de dados (Jugador esperado: {jugador.getNombre()})...");
        string? rfidLeido = _hw.SolicitarRfid(2500); // 2.5s timeout no bloqueante

        if (!string.IsNullOrWhiteSpace(rfidLeido))
        {
            var jugadorRfid = _turnos.BuscarPorRfid(rfidLeido);
            if (jugadorRfid != null)
            {
                Console.WriteLine($"[SERVER] RFID confirmado para: {jugadorRfid.getNombre()}");
            }
            else
            {
                Console.WriteLine($"[SERVER] RFID detectado ({rfidLeido}), pero sin jugador mapeado. Continuando con {jugador.getNombre()}.");
            }
        }
        else
        {
            Console.WriteLine($"[SERVER] Sin RFID / Fallo de hardware. Aplicando fallback automático al jugador de turno: {jugador.getNombre()}.");
        }

        // Tirar dados
        var (d1, d2, total) = _hw.TirarDados();
        _dadosLanzadosEnTurnoActual = true;
        _ultimoDado1 = d1;
        _ultimoDado2 = d2;
        _ultimoTotalDados = total;
        _ultimoValorDisplay = (d1 * 10) + d2; // El display oficial refleja ambos dados (ej: 3 y 4 -> 34)

        // Mover jugador en el tablero circular
        var casillaDestino = _tablero.moverJugadorPorDados(jugador, total, out bool pasoPorSalida);

        if (pasoPorSalida)
        {
            _historial.Registrar(new Transaccion(
                _turnos.GetNumeroRonda(),
                "SALIDA",
                "BANCO",
                jugador.getNombre(),
                200,
                $"{jugador.getNombre()} cruzó la Salida y cobró ₡200."
            ));
            TransmitirATodos($"SALDO_ACTUALIZADO|{jugador.getID()}|{jugador.getSaldo()}");
        }

        // Resolver la casilla (el display de 7 segmentos mantiene visible la tirada de los dados para los jugadores)
        string resultadoCasilla = casillaDestino.aplicarCasilla(jugador, _tablero);

        // Si fue la policía, el jugador fue trasladado a la cárcel (ID 6)
        if (casillaDestino is CasillaPolicia)
        {
            // El jugador fue reubicado a la cárcel por la regla del juego; el dado electrónico permanece en el 7 segmentos
        }
        else if (casillaDestino is CasillaInicial && !pasoPorSalida)
        {
            _historial.Registrar(new Transaccion(
                _turnos.GetNumeroRonda(),
                "SALIDA",
                "BANCO",
                jugador.getNombre(),
                200,
                $"{jugador.getNombre()} cayó en la Salida y cobró ₡200."
            ));
            TransmitirATodos($"SALDO_ACTUALIZADO|{jugador.getID()}|{jugador.getSaldo()}");
        }

        if (resultadoCasilla.StartsWith("PROPIEDAD_DISPONIBLE|"))
        {
            _propiedadPendienteCompra = (Propiedad)casillaDestino;
        }
        else
        {
            _propiedadPendienteCompra = null;
            
            // Si la casilla generó un pago de alquiler
            if (casillaDestino is Propiedad prop && prop.tieneDuenio() && prop.getDuenio() != jugador && !prop.getEstaHipotecada())
            {
                int montoAlquiler = prop.getAlquiler();
                _historial.Registrar(new Transaccion(
                    _turnos.GetNumeroRonda(),
                    "ALQUILER",
                    jugador.getNombre(),
                    prop.getDuenio()!.getNombre(),
                    montoAlquiler,
                    $"{jugador.getNombre()} pagó ₡{montoAlquiler} de alquiler en {prop.getNombre()} a {prop.getDuenio()!.getNombre()}."
                ));
                TransmitirATodos($"SALDO_ACTUALIZADO|{jugador.getID()}|{jugador.getSaldo()}");
                TransmitirATodos($"SALDO_ACTUALIZADO|{prop.getDuenio()!.getID()}|{prop.getDuenio()!.getSaldo()}");
            }
            else if (casillaDestino is CasillaEvento && resultadoCasilla.StartsWith("EVENTO_APLICADO|"))
            {
                var partesEv = resultadoCasilla.Split('|');
                int modMonto = 0;
                if (partesEv.Length >= 5) int.TryParse(partesEv[4], out modMonto);

                string origenEv = modMonto >= 0 ? "BANCO" : jugador.getNombre();
                string destinoEv = modMonto >= 0 ? jugador.getNombre() : "BANCO";

                _historial.Registrar(new Transaccion(
                    _turnos.GetNumeroRonda(),
                    "EVENTO",
                    origenEv,
                    destinoEv,
                    Math.Abs(modMonto),
                    $"{partesEv[1]}: {partesEv[2]}"
                ));
                TransmitirATodos($"SALDO_ACTUALIZADO|{jugador.getID()}|{jugador.getSaldo()}");
            }
        }

        string msg = $"DADOS_LANZADOS|{jugador.getID()}|{d1}|{d2}|{total}|{casillaDestino.getIdCasilla()}|{casillaDestino.getNombre()}|{jugador.getSaldo()}|{resultadoCasilla}";
        writer.WriteLine(msg);
        TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} sacó {d1}+{d2}={total} y cayó en [{casillaDestino.getIdCasilla()}] {casillaDestino.getNombre()}. Saldo: ₡{jugador.getSaldo()}. Detalle: {resultadoCasilla}");
    }

    private (bool ok, string mensaje) ManejarComprarPropiedad(StreamWriter? writer, Jugador? jugador)
    {
        if (jugador == null)
        {
            writer?.WriteLine("ERROR|COMPRA_RECHAZADA|Jugador no identificado.");
            return (false, "Jugador no identificado.");
        }
        if (_turnos.getTurnoActual()?.getID() != jugador.getID())
        {
            writer?.WriteLine("ERROR|COMPRA_RECHAZADA|No es tu turno.");
            return (false, "No es tu turno.");
        }
        if (!_dadosLanzadosEnTurnoActual)
        {
            writer?.WriteLine("ERROR|COMPRA_RECHAZADA|Debe lanzar los dados primero.");
            return (false, "Debe lanzar los dados primero.");
        }
        if (_propiedadPendienteCompra == null)
        {
            writer?.WriteLine("ERROR|COMPRA_RECHAZADA|No hay ninguna propiedad disponible para compra en esta casilla.");
            return (false, "No hay ninguna propiedad disponible para compra en esta casilla.");
        }

        var prop = _propiedadPendienteCompra;
        if (prop.tieneDuenio())
        {
            _propiedadPendienteCompra = null;
            string errDuenio = $"La propiedad {prop.getNombre()} ya tiene dueño ({prop.getDuenio()?.getNombre()}).";
            writer?.WriteLine($"ERROR|COMPRA_RECHAZADA|{errDuenio}");
            Console.WriteLine($"[SERVER] Compra rechazada: {errDuenio}");
            return (false, errDuenio);
        }
        if (jugador.getSaldo() < prop.getPrecioDeCompra())
        {
            string errSaldo = $"Saldo insuficiente. Requiere ₡{prop.getPrecioDeCompra()} pero posee ₡{jugador.getSaldo()}.";
            writer?.WriteLine($"ERROR|COMPRA_RECHAZADA|{errSaldo}");
            Console.WriteLine($"[SERVER] Compra rechazada para {jugador.getNombre()}: {errSaldo}");
            return (false, errSaldo);
        }

        // Procesar compra
        Console.WriteLine($"[SERVER] Solicitud recibida: COMPRAR_PROPIEDAD para {jugador.getNombre()} (ID: {jugador.getID()})");
        Console.WriteLine($"[SERVER] Propiedad: [{prop.getIdCasilla()}] {prop.getNombre()} (Precio: ₡{prop.getPrecioDeCompra()}, Alquiler: ₡{prop.getAlquiler()})");
        Console.WriteLine($"[SERVER] Validando compra... Saldo actual ₡{jugador.getSaldo()} >= ₡{prop.getPrecioDeCompra()} OK.");

        bool exito = prop.comprar(jugador);
        if (exito)
        {
            _propiedadPendienteCompra = null;
            _historial.Registrar(new Transaccion(
                _turnos.GetNumeroRonda(),
                "COMPRA",
                jugador.getNombre(),
                "BANCO",
                prop.getPrecioDeCompra(),
                $"{jugador.getNombre()} compró {prop.getNombre()} por ₡{prop.getPrecioDeCompra()}."
            ));

            Console.WriteLine($"[SERVER] Compra aprobada: {prop.getNombre()} ahora pertenece a {jugador.getNombre()}.");
            Console.WriteLine($"[SERVER] Saldo actualizado: ₡{jugador.getSaldo()}. Transacción registrada.");

            writer?.WriteLine($"COMPRA_EXITOSA|{prop.getIdCasilla()}|{prop.getNombre()}|{prop.getPrecioDeCompra()}|{jugador.getSaldo()}");
            TransmitirATodos($"SALDO_ACTUALIZADO|{jugador.getID()}|{jugador.getSaldo()}");
            TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} ha comprado [{prop.getIdCasilla()}] {prop.getNombre()} por ₡{prop.getPrecioDeCompra()}. Saldo restante: ₡{jugador.getSaldo()}.");
            return (true, $"¡Compra exitosa! Adquiriste {prop.getNombre()} por ₡{prop.getPrecioDeCompra()}. Saldo: ₡{jugador.getSaldo()}.");
        }
        else
        {
            string errFallo = "No se pudo completar la operación de compra.";
            writer?.WriteLine($"ERROR|COMPRA_RECHAZADA|{errFallo}");
            return (false, errFallo);
        }
    }

    private (bool ok, string mensaje) ManejarNoComprar(StreamWriter? writer, Jugador? jugador)
    {
        if (jugador == null) return (false, "No identificado.");
        if (_turnos.getTurnoActual()?.getID() != jugador.getID())
        {
            writer?.WriteLine("ERROR|No es su turno.");
            return (false, "No es su turno.");
        }

        if (_propiedadPendienteCompra != null)
        {
            string nombreProp = _propiedadPendienteCompra.getNombre();
            _propiedadPendienteCompra = null;
            Console.WriteLine($"[SERVER] {jugador.getNombre()} decidió no comprar {nombreProp}.");
            writer?.WriteLine($"OK|Decidió no comprar {nombreProp}.");
            TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} decidió no comprar {nombreProp}.");
            return (true, $"Decidió no comprar {nombreProp}.");
        }
        else
        {
            writer?.WriteLine("OK|Sin compras pendientes.");
            return (true, "Sin compras pendientes.");
        }
    }

    private (bool ok, string mensaje) ManejarTerminarTurno(StreamWriter? writer, Jugador? jugador)
    {
        if (jugador == null) { writer?.WriteLine("ERROR|No identificado."); return (false, "No identificado."); }
        if (_turnos.getTurnoActual()?.getID() != jugador.getID()) { writer?.WriteLine("ERROR|No es su turno."); return (false, "No es su turno."); }
        if (!_dadosLanzadosEnTurnoActual) { writer?.WriteLine("ERROR|Debe lanzar los dados antes de terminar su turno."); return (false, "Debe lanzar los dados antes de terminar su turno."); }

        _dadosLanzadosEnTurnoActual = false;
        _propiedadPendienteCompra = null;

        var siguienteJugador = _turnos.avanzarTurno();
        if (siguienteJugador == null)
        {
            writer?.WriteLine("ERROR|No hay más jugadores activos.");
            return (false, "No hay más jugadores activos.");
        }

        // Registrar fin de turno en el modelo de Juego y evaluar condiciones de victoria
        _juego.RegistrarFinDeTurno();
        if (_juego.PartidaFinalizada)
        {
            TransmitirATodos($"ACTUALIZACION|🏆 ¡PARTIDA FINALIZADA! Ganador: {_juego.Ganador}. Motivo: {_juego.MotivoFinPartida}");
        }
        else if (_turnos.CantidadJugadoresActivos() == 1 && _turnos.GetTotalJugadores() > 1)
        {
            TransmitirATodos($"ACTUALIZACION|🏆 ¡PARTIDA FINALIZADA! ¡{siguienteJugador.getNombre()} ha ganado el Monopoly!");
        }

        // Actualizar el 7 segmentos con el ID del nuevo jugador
        _hw.MostrarEnDisplay(siguienteJugador.getID());

        writer?.WriteLine("TURNO_TERMINADO|OK");
        TransmitirATodos($"NUEVO_TURNO|{siguienteJugador.getID()}|{siguienteJugador.getNombre()}|{siguienteJugador.getSaldo()}|{siguienteJugador.getNodoActual()?.getCasilla().getIdCasilla()}");
        return (true, $"Turno terminado. Turno de {siguienteJugador.getNombre()} (ID: {siguienteJugador.getID()}).");
    }

    private void ManejarHipotecar(StreamWriter writer, Jugador? jugador, string[] partes)
    {
        if (jugador == null || partes.Length < 2 || !int.TryParse(partes[1], out int idCasilla))
        {
            writer.WriteLine("ERROR|Uso: HIPOTECAR|<idCasilla>");
            return;
        }

        var prop = jugador.getPropiedades().buscarPorId(idCasilla);
        if (prop == null) { writer.WriteLine("ERROR|No posee esa propiedad."); return; }

        if (prop.hipotecar())
        {
            _historial.Registrar(new Transaccion(
                _turnos.GetNumeroRonda(),
                "HIPOTECA",
                "BANCO",
                jugador.getNombre(),
                prop.getPrecioDeCompra() / 2,
                $"{jugador.getNombre()} hipotecó {prop.getNombre()} y recibió ₡{prop.getPrecioDeCompra() / 2}."
            ));
            writer.WriteLine($"HIPOTECA_OK|{prop.getNombre()}|{jugador.getSaldo()}");
            TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} hipotecó {prop.getNombre()}. Nuevo saldo: ₡{jugador.getSaldo()}.");
        }
        else
        {
            writer.WriteLine("ERROR|No se puede hipotecar esa propiedad (o ya está hipotecada).");
        }
    }

    private void ManejarDeshipotecar(StreamWriter writer, Jugador? jugador, string[] partes)
    {
        if (jugador == null || partes.Length < 2 || !int.TryParse(partes[1], out int idCasilla))
        {
            writer.WriteLine("ERROR|Uso: DESHIPOTECAR|<idCasilla>");
            return;
        }

        var prop = jugador.getPropiedades().buscarPorId(idCasilla);
        if (prop == null) { writer.WriteLine("ERROR|No posee esa propiedad."); return; }

        if (prop.desHipotecar())
        {
            int costo = (prop.getPrecioDeCompra() / 2) + (prop.getPrecioDeCompra() / 10);
            _historial.Registrar(new Transaccion(
                _turnos.GetNumeroRonda(),
                "DESHIPOTECA",
                jugador.getNombre(),
                "BANCO",
                costo,
                $"{jugador.getNombre()} canceló la hipoteca de {prop.getNombre()} por ₡{costo}."
            ));
            writer.WriteLine($"DESHIPOTECA_OK|{prop.getNombre()}|{jugador.getSaldo()}");
            TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} deshipotecó {prop.getNombre()}. Nuevo saldo: ₡{jugador.getSaldo()}.");
        }
        else
        {
            writer.WriteLine("ERROR|Saldo insuficiente o la propiedad no estaba hipotecada.");
        }
    }

    private void ManejarDesconexionCliente(TcpClient client, Jugador? jugador)
    {
        lock (_lockJuego)
        {
            if (jugador != null)
            {
                Console.WriteLine($"[SERVER] Jugador desconectado: {jugador.getNombre()} (ID: {jugador.getID()})");

                // Si era el turno del jugador que se desconectó, avanzar el turno
                if (_turnos.getTurnoActual()?.getID() == jugador.getID())
                {
                    _dadosLanzadosEnTurnoActual = false;
                    _propiedadPendienteCompra = null;
                    var sig = _turnos.avanzarTurno();
                    if (sig != null && sig.getID() != jugador.getID())
                    {
                        TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} se desconectó durante su turno. Turno cedido a {sig.getNombre()}.");
                        _hw.MostrarEnDisplay(sig.getID());
                    }
                }

                _turnos.eliminarJugador(jugador.getID());
                _rfidEstado.Remove(jugador.getID());

                TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} se ha desconectado. Jugadores en partida: {_turnos.GetTotalJugadores()}/{MAX_JUGADORES}.");
            }

            _clientes.Remove(client);
            try { client.Close(); } catch { }
        }
    }

    private void TransmitirATodos(string mensaje)
    {
        lock (_clientes)
        {
            foreach (var kvp in _clientes)
            {
                if (kvp.Key.Connected)
                {
                    try
                    {
                        var w = new StreamWriter(kvp.Key.GetStream(), Encoding.UTF8) { AutoFlush = true };
                        w.WriteLine(mensaje);
                    }
                    catch { }
                }
            }
        }
    }

    private string GenerarResumenEstado()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Ronda actual: {_turnos.GetNumeroRonda()} | Turno oficial: {_turnos.getTurnoActual()?.getNombre() ?? "Sin jugadores"}");
        sb.AppendLine("Jugadores registrados:");
        
        lock (_clientes)
        {
            foreach (var j in _clientes.Values)
            {
                string estado = j.isActivo() ? (j.isEncarcelado() ? $"[CÁRCEL {j.getTurnosPerdidos()}t]" : "[ACTIVO]") : "[BANCARROTA]";
                sb.AppendLine($"  - [{j.getID()}] {j.getNombre()}: Saldo ₡{j.getSaldo()} | Posición: [{j.getNodoActual()?.getCasilla().getIdCasilla()}] {j.getNodoActual()?.getCasilla().getNombre()} | {estado}");
            }
        }

        return sb.ToString().Replace("\r\n", "; ").Replace("\n", "; ");
    }

    private void MostrarEstadoJuegoEnServidor()
    {
        Console.WriteLine("\n--- ESTADO DEL JUEGO (SERVIDOR) ---");
        Console.WriteLine($"Turno actual: {_turnos.getTurnoActual()?.getNombre() ?? "Ninguno"} (ID: {_turnos.getTurnoActual()?.getID()})");
        Console.WriteLine($"Hardware: {_hw.Estado} en {_hw.PuertoActual}");
        Console.WriteLine("Jugadores:");
        lock (_clientes)
        {
            foreach (var j in _clientes.Values)
            {
                Console.WriteLine($"  ID {j.getID()}: {j.getNombre()} | ₡{j.getSaldo()} | Casilla #{j.getNodoActual()?.getCasilla().getIdCasilla()} ({j.getNodoActual()?.getCasilla().getNombre()}) | Activo={j.isActivo()}");
                Console.WriteLine($"    Propiedades:\n{j.getPropiedades().obtenerListadoTexto()}");
            }
        }
    }

    private void ProbarRfidConsola()
    {
        Console.WriteLine("[PRUEBA RFID] Solicitando lectura (acerque tarjeta en 5s)...");
        string? uid = _hw.SolicitarRfid(5000);
        if (uid != null) Console.WriteLine($"[PRUEBA RFID] UID detectado: {uid}");
        else Console.WriteLine("[PRUEBA RFID] Sin lectura o timeout. El servidor no se bloqueó.");
    }

    private void ProbarDisplayConsola()
    {
        Console.Write("Ingrese número a mostrar en 7 segmentos (00-99): ");
        if (int.TryParse(Console.ReadLine(), out int num))
        {
            _ultimoValorDisplay = num;
            _hw.MostrarEnDisplay(num);
        }
    }

    private void IniciarEscaneoRfidSegundoPlano()
    {
        new Thread(() =>
        {
            while (_corriendo)
            {
                Thread.Sleep(3000);
                if (_hw.Estado != EstadoHardware.CONECTADA) continue;

                // Solo insistir si hay algún jugador esperando RFID
                bool hayEsperando = false;
                lock (_lockJuego)
                {
                    if (_dadosLanzadosEnTurnoActual) continue;
                    foreach (var kvp in _rfidEstado)
                    {
                        if (kvp.Value == "esperando") { hayEsperando = true; break; }
                    }
                }

                if (!hayEsperando) continue;

                // Solicitar RFID de forma no bloqueante
                string? uid = _hw.SolicitarRfid(1500);
                if (!string.IsNullOrWhiteSpace(uid) && uid.Length >= 4)
                {
                    lock (_lockJuego)
                    {
                        Jugador? target = null;
                        if (RFID_CONOCIDOS.TryGetValue(uid, out int idEsperado))
                        {
                            target = _turnos.BuscarPorId(idEsperado);
                        }

                        if (target == null)
                        {
                            // Asociar al primer jugador que esté esperando
                            var n = _turnos.GetActualNodo();
                            for (int i = 0; i < _turnos.GetTotalJugadores(); i++)
                            {
                                if (n != null && _rfidEstado.TryGetValue(n.getJugador().getID(), out var est) && est == "esperando")
                                {
                                    target = n.getJugador();
                                    break;
                                }
                                n = n?.getSiguiente();
                            }
                        }

                        if (target != null)
                        {
                            target.setRfidUid(uid);
                            _rfidEstado[target.getID()] = "identificado";
                            Console.WriteLine($"[SERVER] RFID detectado e identificado: {target.getNombre()} (ID: {target.getID()}) -> UID {uid}");
                            TransmitirATodos($"ACTUALIZACION|✅ RFID DETECTADO: {target.getNombre()} (ID: {target.getID()}) identificado con UID {uid}.");
                        }
                    }
                }
            }
        })
        { IsBackground = true, Name = "EscaneoRfidSegundoPlano" }.Start();
    }

    public string ObtenerEstadoJson()
    {
        lock (_lockJuego)
        {
            var turnoActual = _turnos.getTurnoActual();

            var listaJugadores = new List<object>();
            if (_turnos.GetActualNodo() != null)
            {
                var temp = _turnos.GetActualNodo()!;
                int total = _turnos.GetTotalJugadores();
                for (int i = 0; i < total; i++)
                {
                    var j = temp.getJugador();
                    _rfidEstado.TryGetValue(j.getID(), out string? rEstado);

                    var propsJugador = new List<object>();
                    var nProp = j.getPropiedades().getHead();
                    while (nProp != null)
                    {
                        var p = nProp.getPropiedad();
                        propsJugador.Add(new
                        {
                            id = p.getIdCasilla(),
                            nombre = p.getNombre(),
                            grupo = p.getGrupo(),
                            precio = p.getPrecioDeCompra(),
                            alquiler = p.getAlquiler(),
                            hipotecada = p.getEstaHipotecada()
                        });
                        nProp = nProp.getSiguiente();
                    }

                    listaJugadores.Add(new
                    {
                        id = j.getID(),
                        nombre = j.getNombre(),
                        saldo = j.getSaldo(),
                        casillaId = j.getNodoActual()?.getCasilla().getIdCasilla() ?? 0,
                        casillaNombre = j.getNodoActual()?.getCasilla().getNombre() ?? "Salida",
                        activo = j.isActivo(),
                        encarcelado = j.isEncarcelado(),
                        turnosPerdidos = j.getTurnosPerdidos(),
                        rfidUid = j.getRfidUid() ?? "",
                        rfidEstado = rEstado ?? "no_disponible",
                        propiedades = propsJugador
                    });
                    temp = temp.getSiguiente()!;
                }
            }

            var listaCasillas = new List<object>();
            if (_tablero.getHead() != null)
            {
                var nodo = _tablero.getHead()!;
                for (int i = 0; i < _tablero.getTotalCasillas(); i++)
                {
                    var c = nodo.getCasilla();
                    string tipo = c is Propiedad ? "Propiedad" : (c is CasillaEvento ? "Evento" : "Especial");
                    string grupo = "";
                    int precio = 0;
                    int alquiler = 0;
                    int? duenioId = null;
                    string duenioNombre = "";
                    bool hipotecada = false;

                    if (c is Propiedad prop)
                    {
                        grupo = prop.getGrupo();
                        precio = prop.getPrecioDeCompra();
                        alquiler = prop.getAlquiler();
                        if (prop.tieneDuenio())
                        {
                            duenioId = prop.getDuenio()!.getID();
                            duenioNombre = prop.getDuenio()!.getNombre();
                        }
                        hipotecada = prop.getEstaHipotecada();
                    }

                    listaCasillas.Add(new
                    {
                        id = c.getIdCasilla(),
                        nombre = c.getNombre(),
                        tipo,
                        grupo,
                        precio,
                        alquiler,
                        duenioId,
                        duenioNombre,
                        hipotecada
                    });

                    nodo = nodo.getSiguiente()!;
                }
            }

            var ultimasTx = new List<string>();
            var historialTabla = new List<object>();
            var nodoTx = _historial.GetHead();
            int saltar = Math.Max(0, _historial.GetSize() - 8);
            int idxTx = 0;
            while (nodoTx != null)
            {
                var tx = nodoTx.Transaccion;
                if (idxTx >= saltar)
                {
                    ultimasTx.Add(tx.ToString());
                }
                historialTabla.Add(new
                {
                    ronda = tx.Turno,
                    tipo = tx.Tipo,
                    origen = tx.JugadorOrigen,
                    destino = tx.JugadorDestino,
                    monto = tx.Monto,
                    detalle = tx.Descripcion,
                    fecha = tx.Fecha.ToString("HH:mm:ss")
                });
                idxTx++;
                nodoTx = nodoTx.Siguiente;
            }

            var estadoObj = new
            {
                ronda = _turnos.GetNumeroRonda(),
                turnoActualId = turnoActual?.getID() ?? 0,
                turnoActualNombre = turnoActual?.getNombre() ?? "Esperando jugadores...",
                turnoActualSaldo = turnoActual?.getSaldo() ?? 0,
                turnoActualCasilla = turnoActual?.getNodoActual()?.getCasilla().getIdCasilla() ?? 0,
                dadosLanzados = _dadosLanzadosEnTurnoActual,
                dadosUltimos = new { d1 = _ultimoDado1, d2 = _ultimoDado2, total = _ultimoTotalDados },
                displayValor = _ultimoValorDisplay,
                hardwareEstado = _hw.Estado.ToString(),
                hardwarePuerto = _hw.PuertoActual,
                jugadoresConectados = _turnos.GetTotalJugadores(),
                maxJugadores = MAX_JUGADORES,
                partidaLlena = _turnos.GetTotalJugadores() >= MAX_JUGADORES,
                propiedadPendiente = _propiedadPendienteCompra != null ? new
                {
                    id = _propiedadPendienteCompra.getIdCasilla(),
                    nombre = _propiedadPendienteCompra.getNombre(),
                    precio = _propiedadPendienteCompra.getPrecioDeCompra(),
                    alquiler = _propiedadPendienteCompra.getAlquiler()
                } : null,
                jugadores = listaJugadores,
                casillas = listaCasillas,
                transacciones = ultimasTx,
                historial = historialTabla
            };

            return System.Text.Json.JsonSerializer.Serialize(estadoObj);
        }
    }

    public (bool ok, string mensaje) EjecutarAccionWeb(string accion, int? idCasilla = null, int? idJugador = null, string? nombre = null)
    {
        lock (_lockJuego)
        {
            using var ms = new MemoryStream();
            using var sw = new StreamWriter(ms, Encoding.UTF8) { AutoFlush = true };

            string accionNorm = accion.Trim().ToUpperInvariant();

            // Acciones que no requieren un jugador en turno
            if (accionNorm == "CONECTAR")
            {
                if (_turnos.GetTotalJugadores() >= MAX_JUGADORES)
                {
                    return (false, $"Partida completa ({MAX_JUGADORES}/{MAX_JUGADORES}). No se admiten más jugadores.");
                }

                string nom = !string.IsNullOrWhiteSpace(nombre) ? nombre.Trim() : $"Jugador_{_siguienteIdJugador}";
                var nodoInicio = _tablero.buscarCasillaPorID(0);
                int idAsignado = _siguienteIdJugador++;
                var nuevoJ = new Jugador(idAsignado, nom, nodoInicio, 1500);

                // UID conocido preasignado si existe
                foreach (var kvp in RFID_CONOCIDOS)
                {
                    if (kvp.Value == idAsignado)
                    {
                        nuevoJ.setRfidUid(kvp.Key);
                        break;
                    }
                }

                _rfidEstado[idAsignado] = "esperando";
                _turnos.agregarJugador(nuevoJ);

                _historial.Registrar(new Transaccion(
                    _turnos.GetNumeroRonda(),
                    "CONEXION",
                    "SISTEMA",
                    nuevoJ.getNombre(),
                    1500,
                    $"Jugador {nuevoJ.getNombre()} (ID {nuevoJ.getID()}) ingresó vía Web con ₡1500."
                ));

                if (_turnos.getTurnoActual() != null)
                {
                    _hw.MostrarEnDisplay(_turnos.getTurnoActual()!.getID());
                }

                TransmitirATodos($"ACTUALIZACION|Nuevo jugador conectado vía Web: {nuevoJ.getNombre()} (Saldo: ₡1500). Jugadores: {_turnos.GetTotalJugadores()}/{MAX_JUGADORES}. Turno actual: {_turnos.getTurnoActual()?.getNombre()}");
                return (true, $"¡Bienvenido {nuevoJ.getNombre()}! Asignado ID {idAsignado}. Acerque su tarjeta RFID o elija 'Continuar sin RFID'.");
            }

            if (accionNorm == "CONTINUAR_SIN_RFID")
            {
                int targetId = idJugador ?? _turnos.getTurnoActual()?.getID() ?? 1;
                var jTarget = _turnos.BuscarPorId(targetId);
                if (jTarget == null) return (false, $"Jugador con ID {targetId} no encontrado.");

                _rfidEstado[targetId] = "omitido";
                Console.WriteLine($"[SERVER] {jTarget.getNombre()} (ID: {targetId}) continuará sin RFID (Modo ID).");
                TransmitirATodos($"ACTUALIZACION|{jTarget.getNombre()} (ID: {targetId}) continuará jugando sin RFID (Modo ID).");
                return (true, $"Modo sin RFID activado para {jTarget.getNombre()}. Puede jugar normalmente.");
            }

            if (accionNorm == "REINTENTAR_RFID")
            {
                int targetId = idJugador ?? _turnos.getTurnoActual()?.getID() ?? 1;
                var jTarget = _turnos.BuscarPorId(targetId);
                if (jTarget == null) return (false, $"Jugador con ID {targetId} no encontrado.");

                Console.WriteLine($"[SERVER] Solicitando lectura RFID manual para {jTarget.getNombre()} (ID: {targetId})...");
                string? uid = _hw.SolicitarRfid(3000);
                if (!string.IsNullOrWhiteSpace(uid) && uid.Length >= 4)
                {
                    jTarget.setRfidUid(uid);
                    _rfidEstado[targetId] = "identificado";
                    Console.WriteLine($"[SERVER] RFID detectado exitosamente para {jTarget.getNombre()}: {uid}");
                    TransmitirATodos($"ACTUALIZACION|✅ RFID DETECTADO: {jTarget.getNombre()} (ID: {targetId}) identificado con UID {uid}.");
                    return (true, $"¡RFID Detectado! {jTarget.getNombre()} asociado a UID {uid}.");
                }
                else
                {
                    return (false, "No se detectó ninguna tarjeta RFID. Acerque la tarjeta al lector e intente de nuevo, o presione 'Continuar sin RFID'.");
                }
            }

            var jugador = _turnos.getTurnoActual();
            if (jugador == null) return (false, "No hay jugadores registrados en la partida. Conecte un jugador primero.");

            // Validación de turno si la GUI especifica el jugador que invoca la acción
            if (idJugador.HasValue && idJugador.Value != jugador.getID())
            {
                var jSolicitante = _turnos.BuscarPorId(idJugador.Value);
                string nomSolicitante = jSolicitante?.getNombre() ?? $"Jugador {idJugador.Value}";
                return (false, $"Lanzamiento fuera de turno: Es el turno de {jugador.getNombre()} (ID {jugador.getID()}), no de {nomSolicitante}.");
            }

            switch (accionNorm)
            {
                case "TIRAR_DADOS":
                    if (_dadosLanzadosEnTurnoActual) return (false, "Ya lanzó los dados en este turno.");
                    if (jugador.isEncarcelado())
                    {
                        ManejarTirarDados(sw, jugador);
                        return (true, jugador.getTurnosPerdidos() > 0
                            ? $"Jugador en la cárcel. Le restan {jugador.getTurnosPerdidos()} turnos."
                            : "Cumplió su tiempo en la cárcel y queda libre.");
                    }
                    ManejarTirarDados(sw, jugador);
                    return (true, $"Dados lanzados: {_ultimoDado1} + {_ultimoDado2} = {_ultimoTotalDados}.");

                case "COMPRAR_PROPIEDAD":
                    return ManejarComprarPropiedad(sw, jugador);

                case "NO_COMPRAR":
                    return ManejarNoComprar(sw, jugador);

                case "TERMINAR_TURNO":
                    return ManejarTerminarTurno(sw, jugador);

                case "HIPOTECAR":
                    if (!idCasilla.HasValue) return (false, "Debe indicar el ID de la casilla a hipotecar.");
                    var propH = jugador.getPropiedades().buscarPorId(idCasilla.Value);
                    if (propH == null) return (false, "No posee esa propiedad.");
                    if (propH.hipotecar())
                    {
                        _historial.Registrar(new Transaccion(
                            _turnos.GetNumeroRonda(),
                            "HIPOTECA",
                            "BANCO",
                            jugador.getNombre(),
                            propH.getPrecioDeCompra() / 2,
                            $"{jugador.getNombre()} hipotecó {propH.getNombre()} y recibió ₡{propH.getPrecioDeCompra() / 2}."
                        ));
                        TransmitirATodos($"SALDO_ACTUALIZADO|{jugador.getID()}|{jugador.getSaldo()}");
                        TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} hipotecó {propH.getNombre()}. Nuevo saldo: ₡{jugador.getSaldo()}.");
                        return (true, $"Propiedad {propH.getNombre()} hipotecada exitosamente. Recibió ₡{propH.getPrecioDeCompra() / 2}.");
                    }
                    return (false, "No se puede hipotecar esa propiedad (ya está hipotecada o tiene mejoras).");

                case "DESHIPOTECAR":
                    if (!idCasilla.HasValue) return (false, "Debe indicar el ID de la casilla a deshipotecar.");
                    var propD = jugador.getPropiedades().buscarPorId(idCasilla.Value);
                    if (propD == null) return (false, "No posee esa propiedad.");
                    int costoD = (propD.getPrecioDeCompra() / 2) + (propD.getPrecioDeCompra() / 10);
                    if (jugador.getSaldo() < costoD) return (false, $"Saldo insuficiente para deshipotecar (Requiere ₡{costoD}, posee ₡{jugador.getSaldo()}).");
                    if (propD.desHipotecar())
                    {
                        _historial.Registrar(new Transaccion(
                            _turnos.GetNumeroRonda(),
                            "DESHIPOTECA",
                            jugador.getNombre(),
                            "BANCO",
                            costoD,
                            $"{jugador.getNombre()} canceló la hipoteca de {propD.getNombre()} por ₡{costoD}."
                        ));
                        TransmitirATodos($"SALDO_ACTUALIZADO|{jugador.getID()}|{jugador.getSaldo()}");
                        TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} deshipotecó {propD.getNombre()}. Nuevo saldo: ₡{jugador.getSaldo()}.");
                        return (true, $"Propiedad {propD.getNombre()} deshipotecada con éxito. Costo: ₡{costoD}.");
                    }
                    return (false, "No se pudo deshipotecar la propiedad.");

                default:
                    return (false, $"Acción '{accion}' desconocida.");
            }
        }
    }
}