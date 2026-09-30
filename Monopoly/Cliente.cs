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
                        ProcesarMensajeServidor(linea);
                    }
                    catch
                    {
                        break;
                    }
                }
                Console.WriteLine("\n[CLIENTE] Conexión perdida con el servidor.");
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
                        writer.WriteLine("TIRAR_DADOS");
                        break;
                    case "2":
                        writer.WriteLine("COMPRAR_PROPIEDAD");
                        break;
                    case "3":
                        writer.WriteLine("NO_COMPRAR");
                        break;
                    case "4":
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
                    string detalle = partes.Length > 8 ? partes[8] : "";

                    if (idJugador == _miId)
                    {
                        _miSaldo = saldo;
                        _dadosLanzadosEsteTurno = true;
                        Console.WriteLine($"\n🎲 [TUS DADOS] Sacaste {d1} + {d2} = {tot}.");
                        Console.WriteLine($"📍 [POSICIÓN] Llegaste a [{idCasilla}] {nomCasilla}. Tu saldo oficial: ₡{_miSaldo}.");
                        
                        if (detalle.StartsWith("PROPIEDAD_DISPONIBLE"))
                        {
                            var tokens = detalle.Split('|');
                            _propiedadDisponibleParaComprar = true;
                            _infoPropiedad = $"{tokens[2]} (Precio: ₡{tokens[3]}, Alquiler: ₡{tokens[4]})";
                            Console.WriteLine($"🏠 [PROPIEDAD DISPONIBLE] Puedes comprar {_infoPropiedad} usando la Opción 2 o pasar con la Opción 3.");
                        }
                        else
                        {
                            Console.WriteLine($"ℹ️  [RESULTADO CASILLA] {detalle}");
                        }
                    }
                }
                break;

            case "COMPRA_EXITOSA":
                if (partes.Length >= 5)
                {
                    _miSaldo = int.Parse(partes[4]);
                    _propiedadDisponibleParaComprar = false;
                    Console.WriteLine($"\n🎉 [COMPRA EXITOSA] Has adquirido [{partes[1]}] {partes[2]} por ₡{partes[3]}. Nuevo saldo: ₡{_miSaldo}.");
                }
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
                    Console.WriteLine($"\n❌ [ERROR SERVIDOR] {partes[1]}");
                }
                break;

            case "INFO":
            case "OK":
                if (partes.Length > 1)
                {
                    Console.WriteLine($"\nℹ️  [SERVIDOR] {partes[1]}");
                }
                break;

            default:
                Console.WriteLine($"\n[RED RAW] {mensaje}");
                break;
        }

        Console.Write("Opción> ");
    }
}