using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MonopolyDistribuido;

public class Cliente
{
    private static int _miId = -1;
    private static string _miNombre = "";
    private static int _miSaldo = 0;
    private static int _turnoActualId = -1;
    private static bool _dadosLanzadosEsteTurno = false;
    private static bool _propiedadDisponibleParaComprar = false;
    private static string _infoPropiedad = "";

    public static void Conectar(string ip, int puerto, string? nombreJugador = null)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine($"[CLIENTE] Conectando a {ip}:{puerto}...");
        Console.WriteLine("=================================================");

        try
        {
            using var socket = new TcpClient(ip, puerto);
            using var stream = socket.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            if (string.IsNullOrWhiteSpace(nombreJugador))
            {
                Console.Write("Ingrese su nombre de jugador: ");
                nombreJugador = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(nombreJugador)) nombreJugador = "Jugador_" + new Random().Next(100, 999);
            }

            _miNombre = nombreJugador;
            writer.WriteLine($"CONECTAR|{_miNombre}");

            // Hilo en segundo plano para escuchar eventos del Servidor
            var hiloEscucha = new Thread(() =>
            {
                while (socket.Connected)
                {
                    try
                    {
                        var linea = reader.ReadLine();
                        if (linea == null) break;
                        try
                        {
                            ProcesarMensajeServidor(linea);
                        }
                        catch (Exception exProc)
                        {
                            Console.WriteLine($"\n[CLIENTE AVISO] Error procesando evento ({exProc.Message})");
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
                Console.WriteLine("\n[CLIENTE] Conexión cerrada con el servidor.");
            })
            { IsBackground = true, Name = "ClienteReceptorThread" };
            hiloEscucha.Start();

            // Esperar brevemente a recibir el mensaje de bienvenida inicial
            Thread.Sleep(300);

            // Bucle del menú del cliente
            while (socket.Connected)
            {
                bool esMiTurno = (_miId > 0 && _turnoActualId == _miId);

                Console.WriteLine($"\n-------------------------------------------------");
                Console.WriteLine($" JUGADOR: {_miNombre} (ID: {_miId}) | SALDO: ₡{_miSaldo}");
                Console.WriteLine(esMiTurno ? " >>> ¡ES TU TURNO! <<<" : $" [Esperando turno del Jugador {_turnoActualId}]");
                Console.WriteLine($"-------------------------------------------------");
                if (esMiTurno)
                {
                    if (!_dadosLanzadosEsteTurno)
                    {
                        Console.WriteLine(" 👉 Se recomienda lanzar los dados primero (Opción 1).");
                    }
                    else if (_propiedadDisponibleParaComprar)
                    {
                        Console.WriteLine($" 👉 ¡Propiedad disponible para compra! ({_infoPropiedad}).");
                        Console.WriteLine("    Usa Opción 2 para comprarla, o Opción 3 para no comprar.");
                    }
                    else
                    {
                        Console.WriteLine(" 👉 Ya lanzaste dados. Puedes terminar tu turno con la Opción 4.");
                    }
                }
                Console.WriteLine("Acciones disponibles:");
                Console.WriteLine("  1. Tirar Dados");
                Console.WriteLine("  2. Comprar Propiedad en casilla actual");
                Console.WriteLine("  3. No Comprar / Pasar compra");
                Console.WriteLine("  4. Terminar Turno");
                Console.WriteLine("  5. Hipotecar Propiedad");
                Console.WriteLine("  6. Deshipotecar Propiedad");
                Console.WriteLine("  7. Consultar Estado Oficial del Juego");
                Console.WriteLine("  8. Consultar Historial de Transacciones");
                Console.WriteLine("  9. Salir");
                Console.Write("Opción> ");

                var op = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(op)) continue;

                switch (op)
                {
                    case "1":
                        Console.WriteLine("[CLIENTE] Solicitud enviada: TIRAR_DADOS...");
                        writer.WriteLine("TIRAR_DADOS");
                        break;
                    case "2":
                        if (!_propiedadDisponibleParaComprar)
                        {
                            Console.WriteLine("⚠️ No hay ninguna propiedad disponible para comprar en esta casilla.");
                        }
                        else
                        {
                            Console.WriteLine($"[CLIENTE] Solicitud: COMPRAR_PROPIEDAD ({_infoPropiedad})");
                            writer.WriteLine("COMPRAR_PROPIEDAD");
                        }
                        break;
                    case "3":
                        Console.WriteLine("[CLIENTE] Solicitud: NO_COMPRAR...");
                        writer.WriteLine("NO_COMPRAR");
                        _propiedadDisponibleParaComprar = false;
                        _infoPropiedad = "";
                        break;
                    case "4":
                        Console.WriteLine("[CLIENTE] Solicitud: TERMINAR_TURNO...");
                        writer.WriteLine("TERMINAR_TURNO");
                        break;
                    case "5":
                        Console.Write("Ingrese ID de la casilla a hipotecar: ");
                        var idHip = Console.ReadLine()?.Trim();
                        writer.WriteLine($"HIPOTECAR|{idHip}");
                        break;
                    case "6":
                        Console.Write("Ingrese ID de la casilla a deshipotecar: ");
                        var idDes = Console.ReadLine()?.Trim();
                        writer.WriteLine($"DESHIPOTECAR|{idDes}");
                        break;
                    case "7":
                        writer.WriteLine("CONSULTAR_ESTADO");
                        break;
                    case "8":
                        writer.WriteLine("CONSULTAR_TRANSACCIONES");
                        break;
                    case "9":
                        Console.WriteLine("Desconectando...");
                        return;
                    default:
                        Console.WriteLine("Opción no válida.");
                        break;
                }

                Thread.Sleep(300); // Pausa breve para esperar respuesta del servidor
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLIENTE ERROR] No se pudo conectar al servidor: {ex.Message}");
        }
    }

    private static void ProcesarMensajeServidor(string mensaje)
    {
        var partes = mensaje.Split('|');
        string tipo = partes[0].ToUpperInvariant();

        switch (tipo)
        {
            case "BIENVENIDO":
                if (partes.Length >= 5)
                {
                    _miId = int.Parse(partes[1]);
                    _miNombre = partes[2];
                    _miSaldo = int.Parse(partes[3]);
                    int totalCasillas = int.Parse(partes[4]);
                    if (partes.Length >= 6 && int.TryParse(partes[5], out int tAct))
                    {
                        _turnoActualId = tAct;
                    }
                    Console.WriteLine($"\n[RED] ¡Registrado exitosamente! ID={_miId}, Saldo inicial=₡{_miSaldo}, Tablero={totalCasillas} casillas.");
                }
                break;

            case "NUEVO_TURNO":
                if (partes.Length >= 3)
                {
                    _turnoActualId = int.Parse(partes[1]);
                    string nombreTurno = partes[2];
                    _dadosLanzadosEsteTurno = false;
                    _propiedadDisponibleParaComprar = false;
                    _infoPropiedad = "";

                    if (_turnoActualId == _miId)
                    {
                        Console.WriteLine($"\n🔔 [ATENCIÓN] ¡Ha iniciado tu turno! Puedes lanzar los dados (Opción 1).");
                    }
                    else
                    {
                        Console.WriteLine($"\n[TURNO] Ahora es el turno de: {nombreTurno} (ID: {_turnoActualId}).");
                    }
                }
                break;

            case "DADOS_LANZADOS":
                if (partes.Length >= 8)
                {
                    int idJugador = int.Parse(partes[1]);
                    int d1 = int.Parse(partes[2]);
                    int d2 = int.Parse(partes[3]);
                    int tot = int.Parse(partes[4]);
                    int idCasilla = int.Parse(partes[5]);
                    string nomCasilla = partes[6];
                    int saldo = int.Parse(partes[7]);

                    if (idJugador == _miId)
                    {
                        _miSaldo = saldo;
                        _dadosLanzadosEsteTurno = true;
                        Console.WriteLine($"\n🎲 [TUS DADOS] Sacaste {d1} + {d2} = {tot}.");
                        Console.WriteLine($"📍 [POSICIÓN] Llegaste a [{idCasilla}] {nomCasilla}. Tu saldo oficial: ₡{_miSaldo}.");
                        
                        // Si la casilla destino es una propiedad disponible, partes contiene:
                        // partes[8] = "PROPIEDAD_DISPONIBLE"
                        // partes[9] = idProp
                        // partes[10] = nombreProp
                        // partes[11] = precio
                        // partes[12] = alquiler
                        if (partes.Length >= 13 && partes[8] == "PROPIEDAD_DISPONIBLE")
                        {
                            int propId = int.Parse(partes[9]);
                            string propNom = partes[10];
                            int propPrecio = int.Parse(partes[11]);
                            int propAlquiler = int.Parse(partes[12]);

                            _propiedadDisponibleParaComprar = true;
                            _infoPropiedad = $"{propNom} (Precio: ₡{propPrecio}, Alquiler: ₡{propAlquiler})";

                            Console.WriteLine($"\n╔════════════════════════════════════════════════╗");
                            Console.WriteLine($"║          🏠 ¡PROPIEDAD DISPONIBLE!             ║");
                            Console.WriteLine($"╠════════════════════════════════════════════════╣");
                            Console.WriteLine($"║  Nombre:     {propNom.PadRight(32)}  ║");
                            Console.WriteLine($"║  Precio:     ₡{propPrecio.ToString().PadRight(31)} ║");
                            Console.WriteLine($"║  Alquiler:   ₡{propAlquiler.ToString().PadRight(31)} ║");
                            Console.WriteLine($"║  Tu Saldo:   ₡{_miSaldo.ToString().PadRight(31)} ║");
                            Console.WriteLine($"╚════════════════════════════════════════════════╝");
                            Console.WriteLine($"👉 Usa Opción 2 para COMPRAR o Opción 3 para NO COMPRAR.");
                        }
                        else
                        {
                            _propiedadDisponibleParaComprar = false;
                            _infoPropiedad = "";
                            string detalle = partes.Length > 8 ? string.Join(" ", System.Linq.Enumerable.Skip(partes, 8)) : "";
                            if (!string.IsNullOrWhiteSpace(detalle))
                            {
                                Console.WriteLine($"ℹ️  [RESULTADO CASILLA] {detalle}");
                            }
                        }
                    }
                }
                break;

            case "COMPRA_EXITOSA":
                if (partes.Length >= 5)
                {
                    _miSaldo = int.Parse(partes[4]);
                    _propiedadDisponibleParaComprar = false;
                    _infoPropiedad = "";
                    Console.WriteLine($"\n🎉 [COMPRA EXITOSA] ¡Has adquirido [{partes[1]}] {partes[2]} por ₡{partes[3]}!");
                    Console.WriteLine($"💰 Tu nuevo saldo oficial es: ₡{_miSaldo}.");
                }
                break;

            case "TURNO_TERMINADO":
                _dadosLanzadosEsteTurno = false;
                _propiedadDisponibleParaComprar = false;
                _infoPropiedad = "";
                Console.WriteLine("\n✅ [TURNO FINALIZADO] Tu turno ha concluido exitosamente.");
                break;

            case "SALDO_ACTUALIZADO":
                if (partes.Length >= 3 && int.TryParse(partes[1], out int idJ) && int.TryParse(partes[2], out int sldJ))
                {
                    if (idJ == _miId)
                    {
                        _miSaldo = sldJ;
                    }
                }
                break;

            case "ACTUALIZACION":
                if (partes.Length > 1)
                {
                    Console.WriteLine($"\n📢 [JUEGO] {partes[1]}");
                }
                break;

            case "ESTADO":
                if (partes.Length > 1)
                {
                    Console.WriteLine($"\n📊 [ESTADO DEL JUEGO]:\n{partes[1].Replace("; ", "\n")}");
                }
                break;

            case "TRANSACCIONES":
                if (partes.Length > 1)
                {
                    Console.WriteLine($"\n📜 [HISTORIAL DE TRANSACCIONES]:\n{partes[1]}");
                }
                break;

            case "ERROR":
                if (partes.Length > 1)
                {
                    if (partes.Length > 2 && partes[1] == "COMPRA_RECHAZADA")
                    {
                        Console.WriteLine($"\n❌ [COMPRA RECHAZADA] {partes[2]}");
                        _propiedadDisponibleParaComprar = false;
                        _infoPropiedad = "";
                    }
                    else
                    {
                        Console.WriteLine($"\n❌ [ERROR SERVIDOR] {partes[1]}");
                        if (partes[1].Contains("COMPRA_RECHAZADA") || partes[1].Contains("Saldo insuficiente") || partes[1].Contains("ya tiene dueño"))
                        {
                            _propiedadDisponibleParaComprar = false;
                            _infoPropiedad = "";
                        }
                    }
                }
                break;

            case "INFO":
            case "OK":
                if (partes.Length > 1)
                {
                    Console.WriteLine($"\n✅ [SERVIDOR] {partes[1]}");
                    if (partes[1].Contains("no comprar") || partes[1].Contains("Sin compras pendientes"))
                    {
                        _propiedadDisponibleParaComprar = false;
                        _infoPropiedad = "";
                    }
                }
                break;

            default:
                Console.WriteLine($"\n[RED RAW] {mensaje}");
                break;
        }

        Console.Write("Opción> ");
    }
}