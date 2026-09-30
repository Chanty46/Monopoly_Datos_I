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
    private readonly TcpListener _listener;
    private readonly ControladorHardware _hw;
    private readonly Tablero _tablero;
    private readonly ListaTurnos _turnos;
    private readonly HistorialTransacciones _historial;
    private readonly Dictionary<TcpClient, Jugador> _clientes = new();
    private readonly object _lockJuego = new();
    private bool _corriendo = true;
    private int _siguienteIdJugador = 1;

    // Estado del turno activo
    private bool _dadosLanzadosEnTurnoActual = false;
    private Propiedad? _propiedadPendienteCompra = null;

    public Servidor(int puertoTcp, ControladorHardware hardware)
    {
        _hw = hardware;
        _tablero = new Tablero();
        _tablero.InicializarTablero24();
        _turnos = new ListaTurnos();
        _historial = new HistorialTransacciones("transacciones.txt");
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
                else if (op == "5") { _corriendo = false; _listener.Stop(); _hw.Detener(); break; }
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
        string nombre = partes.Length > 1 && !string.IsNullOrWhiteSpace(partes[1]) ? partes[1].Trim() : $"Jugador_{_siguienteIdJugador}";
        
        var nodoInicio = _tablero.buscarCasillaPorID(0); // Salida
        jugador = new Jugador(_siguienteIdJugador++, nombre, nodoInicio, 1500);

        // Si tenemos asignación de tarjetas RFID conocidas, se puede asociar
        if (partes.Length > 2 && !string.IsNullOrWhiteSpace(partes[2]))
        {
            jugador.setRfidUid(partes[2].Trim());
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

        Console.WriteLine($"[SERVER] Jugador registrado: {jugador.getNombre()} (ID: {jugador.getID()})");

        // Informar al cliente que se unió
        writer.WriteLine($"BIENVENIDO|{jugador.getID()}|{jugador.getNombre()}|{jugador.getSaldo()}|{_tablero.getTotalCasillas()}|{_turnos.getTurnoActual()?.getID()}");

        // Actualizar el display con el jugador actual en turno
        if (_turnos.getTurnoActual() != null)
        {
            _hw.MostrarEnDisplay(_turnos.getTurnoActual()!.getID());
        }

        // Difundir a todos los clientes
        TransmitirATodos($"ACTUALIZACION|Nuevo jugador conectado: {jugador.getNombre()} (Saldo: ₡{jugador.getSaldo()}). Turno actual: {_turnos.getTurnoActual()?.getNombre()}");
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

        // Mostrar en display la posición destino inicial
        _hw.MostrarEnDisplay(casillaDestino.getIdCasilla());

        // Resolver la casilla
        string resultadoCasilla = casillaDestino.aplicarCasilla(jugador, _tablero);

        // Si fue la policía, el jugador fue trasladado a la cárcel (ID 6)
        if (casillaDestino is CasillaPolicia)
        {
            _hw.MostrarEnDisplay(jugador.getNodoActual()?.getCasilla().getIdCasilla() ?? 6);
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

    private void ManejarComprarPropiedad(StreamWriter writer, Jugador? jugador)
    {
        if (jugador == null) { writer.WriteLine("ERROR|No identificado."); return; }
        if (_turnos.getTurnoActual()?.getID() != jugador.getID()) { writer.WriteLine("ERROR|No es su turno."); return; }
        if (!_dadosLanzadosEnTurnoActual) { writer.WriteLine("ERROR|Debe lanzar los dados primero."); return; }
        if (_propiedadPendienteCompra == null) { writer.WriteLine("ERROR|No hay ninguna propiedad disponible para compra en esta casilla."); return; }

        var prop = _propiedadPendienteCompra;
        if (prop.tieneDuenio()) { writer.WriteLine("ERROR|La propiedad ya tiene dueño."); return; }
        if (jugador.getSaldo() < prop.getPrecioDeCompra()) { writer.WriteLine("ERROR|Saldo insuficiente para comprar esta propiedad."); return; }

        // Procesar compra
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

            writer.WriteLine($"COMPRA_EXITOSA|{prop.getIdCasilla()}|{prop.getNombre()}|{prop.getPrecioDeCompra()}|{jugador.getSaldo()}");
            TransmitirATodos($"SALDO_ACTUALIZADO|{jugador.getID()}|{jugador.getSaldo()}");
            TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} ha comprado [{prop.getIdCasilla()}] {prop.getNombre()} por ₡{prop.getPrecioDeCompra()}. Saldo restante: ₡{jugador.getSaldo()}.");
        }
        else
        {
            writer.WriteLine("ERROR|No se pudo completar la compra.");
        }
    }

    private void ManejarNoComprar(StreamWriter writer, Jugador? jugador)
    {
        if (jugador == null) return;
        if (_turnos.getTurnoActual()?.getID() != jugador.getID()) { writer.WriteLine("ERROR|No es su turno."); return; }

        if (_propiedadPendienteCompra != null)
        {
            string nombreProp = _propiedadPendienteCompra.getNombre();
            _propiedadPendienteCompra = null;
            writer.WriteLine("OK|Decidió no comprar.");
            TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} decidió no comprar {nombreProp}.");
        }
        else
        {
            writer.WriteLine("OK|Sin compras pendientes.");
        }
    }

    private void ManejarTerminarTurno(StreamWriter writer, Jugador? jugador)
    {
        if (jugador == null) { writer.WriteLine("ERROR|No identificado."); return; }
        if (_turnos.getTurnoActual()?.getID() != jugador.getID()) { writer.WriteLine("ERROR|No es su turno."); return; }
        if (!_dadosLanzadosEnTurnoActual) { writer.WriteLine("ERROR|Debe lanzar los dados antes de terminar su turno."); return; }

        _dadosLanzadosEnTurnoActual = false;
        _propiedadPendienteCompra = null;

        var siguienteJugador = _turnos.avanzarTurno();
        if (siguienteJugador == null)
        {
            writer.WriteLine("ERROR|No hay más jugadores activos.");
            return;
        }

        // Verificar si solo queda un jugador activo (ganador de la partida)
        if (_turnos.CantidadJugadoresActivos() == 1 && _turnos.GetTotalJugadores() > 1)
        {
            TransmitirATodos($"ACTUALIZACION|🏆 ¡PARTIDA FINALIZADA! ¡{siguienteJugador.getNombre()} ha ganado el Monopoly!");
        }

        // Actualizar el 7 segmentos con el ID del nuevo jugador
        _hw.MostrarEnDisplay(siguienteJugador.getID());

        writer.WriteLine("TURNO_TERMINADO|OK");
        TransmitirATodos($"NUEVO_TURNO|{siguienteJugador.getID()}|{siguienteJugador.getNombre()}|{siguienteJugador.getSaldo()}|{siguienteJugador.getNodoActual()?.getCasilla().getIdCasilla()}");
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
                    if (sig != null)
                    {
                        TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} se desconectó durante su turno. Turno cedido a {sig.getNombre()}.");
                        _hw.MostrarEnDisplay(sig.getID());
                    }
                }
                else
                {
                    TransmitirATodos($"ACTUALIZACION|{jugador.getNombre()} se ha desconectado.");
                }
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
            _hw.MostrarEnDisplay(num);
        }
    }
}