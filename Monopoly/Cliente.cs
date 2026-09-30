using System.Net.Sockets;
using System.Text;

namespace MonopolyDistribuido;

// ============================================================================
// CLIENTE
// No contiene logica de juego: solo envia solicitudes al servidor y muestra lo que
// el servidor informa. No puede modificar saldo, posicion, propiedades ni turno.
// ============================================================================
public static class Cliente
{
    private static readonly object consola = new();
    private static volatile string miNombre = "";

    public static void Conectar(string ip, int puerto)
    {
        try
        {
            using var socket = new TcpClient();
            socket.Connect(ip, puerto);
            socket.NoDelay = true;

            var utf8 = new UTF8Encoding(false);
            using var stream = socket.GetStream();
            using var lector = new StreamReader(stream, utf8);
            using var escritor = new StreamWriter(stream, utf8) { AutoFlush = true, NewLine = "\n" };

            Console.WriteLine($"[CLIENTE] Conectado a {ip}:{puerto}");

            // Hilo que recibe TODO lo que el servidor manda (respuestas y actualizaciones)
            new Thread(() => EscucharServidor(lector)) { IsBackground = true }.Start();

            Console.Write("Nombre de jugador: ");
            string nombre = (Console.ReadLine() ?? "").Trim();
            escritor.WriteLine(Protocolo.Armar(Protocolo.CONECTAR, nombre));

            BucleMenu(escritor);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[CLIENTE ERROR] No se pudo conectar a {ip}:{puerto}: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[CLIENTE] Conexion perdida: {ex.Message}");
        }
    }

    // ------------------------------------------------------------------------
    // Menu del jugador
    // ------------------------------------------------------------------------
    private static void BucleMenu(StreamWriter w)
    {
        while (true)
        {
            lock (consola)
            {
                Console.WriteLine("\n1 Tirar dados | 2 Comprar | 3 No comprar | 4 Terminar turno | " +
                                  "5 Estado | 6 Historial | 7 Registrar tarjeta RFID | 0 Salir");
                Console.Write("Opcion> ");
            }

            string? op = Console.ReadLine();
            if (op == null) return;

            switch (op.Trim())
            {
                case "1": w.WriteLine(Protocolo.TIRAR_DADOS); break;
                case "2": w.WriteLine(Protocolo.COMPRAR_PROPIEDAD); break;
                case "3": w.WriteLine(Protocolo.NO_COMPRAR); break;
                case "4": w.WriteLine(Protocolo.TERMINAR_TURNO); break;
                case "5": w.WriteLine(Protocolo.CONSULTAR_ESTADO); break;
                case "6": SubmenuHistorial(w); break;
                case "7": w.WriteLine(Protocolo.REGISTRAR_TARJETA); break;
                case "0": w.WriteLine(Protocolo.SALIR); return;
                default: Console.WriteLine("Opcion invalida."); break;
            }
        }
    }

    private static void SubmenuHistorial(StreamWriter w)
    {
        Console.WriteLine("Historial: 1 Desde la mas antigua | 2 Desde la mas reciente | 3 Por jugador | 4 Por tipo");
        Console.Write("Opcion> ");
        switch ((Console.ReadLine() ?? "").Trim())
        {
            case "1": w.WriteLine(Protocolo.Armar(Protocolo.CONSULTAR_TRANSACCIONES, "ANTIGUAS")); break;
            case "2": w.WriteLine(Protocolo.Armar(Protocolo.CONSULTAR_TRANSACCIONES, "RECIENTES")); break;
            case "3":
                Console.Write("Nombre del jugador (o Banco): ");
                w.WriteLine(Protocolo.Armar(Protocolo.CONSULTAR_TRANSACCIONES, "JUGADOR", Console.ReadLine() ?? ""));
                break;
            case "4":
                Console.WriteLine("Tipos: CompraPropiedad, PagoAlquiler, PagoAlBanco, PagoEntreJugadores, " +
                                  "GananciaPorEvento, PerdidaPorEvento, PremioPasarInicio");
                Console.Write("Tipo: ");
                w.WriteLine(Protocolo.Armar(Protocolo.CONSULTAR_TRANSACCIONES, "TIPO", Console.ReadLine() ?? ""));
                break;
            default: Console.WriteLine("Opcion invalida."); break;
        }
    }

    // ------------------------------------------------------------------------
    // Recepcion de mensajes del servidor
    // ------------------------------------------------------------------------
    private static void EscucharServidor(StreamReader lector)
    {
        try
        {
            string? linea;
            while ((linea = lector.ReadLine()) != null)
            {
                string texto = Formatear(linea);
                lock (consola)
                {
                    Console.WriteLine($"\n{texto}");
                    Console.Write("Opcion> ");
                }
            }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }

        lock (consola) { Console.WriteLine("\n[CLIENTE] El servidor cerro la conexion."); }
        Environment.Exit(0);
    }

    // Convierte un mensaje del protocolo en texto legible
    private static string Formatear(string linea)
    {
        string[] p = Protocolo.Partir(linea);
        string C(int i) => i < p.Length ? p[i] : "";

        switch (p[0])
        {
            case "BIENVENIDO": return $"[RED] Conectado. La partida es de {C(1)} jugadores.";
            case "CONECTADO": miNombre = C(1); return $"[RED] Registrado como '{C(1)}'. Esperando a los demas jugadores...";
            case "JUGADOR_UNIDO": return $"[LOBBY] {C(1)} se unio a la partida ({C(2)}/{C(3)}).";
            case "JUGADOR_SALIO": return $"[LOBBY] {C(1)} salio ({C(2)}/{C(3)}).";
            case "INICIO": return $"=== LA PARTIDA COMENZO: {C(1)} jugadores, saldo inicial ${C(2)} ===";
            case "TURNO":
                return C(2) == miNombre
                    ? $"[TURNO #{C(3)}] >>> ES TU TURNO, {C(2)} <<<"
                    : $"[TURNO #{C(3)}] Juega {C(2)}.";
            case "DADOS": return $"[DADOS] {C(2)} lanzo: [{C(3)}] [{C(4)}] = {SafeSuma(C(3), C(4))}";
            case "MOVER": return $"[MOVER] {C(2)}: casilla {C(3)} -> '{C(5)}' (#{C(4)})";
            case "REUBICADO": return $"[MOVER] {C(2)} fue reubicado a '{C(4)}' (#{C(3)})";
            case "EVENTO": return $"[EVENTO] {C(2)} tomo una carta: {C(3)}";
            case "ENCARCELADO": return $"[CARCEL] {C(2)} fue enviado a la carcel.";
            case "EN_CARCEL": return $"[CARCEL] {C(2)} cumple sancion. Turnos restantes: {C(3)}";
            case "LIBERADO": return $"[CARCEL] {C(2)} quedo libre.";
            case "PUEDE_COMPRAR":
                return C(2) == miNombre
                    ? $"[COMPRA] Puedes comprar '{C(4)}' por ${C(5)}. Use 2 (comprar) o 3 (no comprar)."
                    : $"[COMPRA] {C(2)} puede comprar '{C(4)}' por ${C(5)}.";
            case "COMPRA": return $"[COMPRA] {C(2)} compro '{C(4)}' por ${C(5)}.";
            case "NO_COMPRA": return $"[COMPRA] {C(2)} no compro '{C(3)}'.";
            case "FIN_TURNO": return $"[TURNO] {C(2)} termino su turno.";
            case "NUEVA_TX": return $"[TRANSACCION] Turno {C(1)} | {C(2)} | {C(3)} -> {C(4)} | ${C(5)} | {C(6)}";
            case "BANCARROTA": return $"[BANCARROTA] {C(2)} quedo ELIMINADO (acreedor: {C(3)}; motivo: {C(4)}).";
            case "DESCONECTADO": return $"[RED] {C(2)} se desconecto.";
            case "PATRIMONIO": return $"[PATRIMONIO] {C(2)}: ${C(3)}";
            case "GANADOR": return $"\n*** GANADOR: {C(2)} con patrimonio ${C(3)} ({C(4)}) ***";
            case "FIN_PARTIDA": return $"[FIN] {C(1)}";
            case "INFO": return $"[INFO] {C(1).Replace('_', ' ')}";
            case "TARJETA_REGISTRADA": return $"[RFID] Tarjeta {C(2)} asociada a {C(1)}.";
            case "LOBBY": return $"[LOBBY] {C(1)}/{C(2)} jugadores conectados: {C(3)}";
            case "ERROR": return $"[ERROR] {C(1)}: {C(2)}";
            case "ESTADO": return FormatearEstado(p);
            case "DUENIOS": return FormatearDuenios(C(1));
            case "TX":
                return $"  #{C(1)} | Turno {C(2)} | {C(3)} | {C(4)} | {C(5)} -> {C(6)} | ${C(7)} | {C(8)}";
            case "TX_FIN": return $"[HISTORIAL] {C(2)} transaccion(es) ({C(1)}).";
            default: return $"[RED] {linea}";
        }
    }

    private static string SafeSuma(string a, string b)
    {
        return int.TryParse(a, out int x) && int.TryParse(b, out int y) ? (x + y).ToString() : "?";
    }

    private static string FormatearEstado(string[] p)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"----- ESTADO (turno #{(p.Length > 1 ? p[1] : "?")}, juega: {(p.Length > 2 ? p[2] : "?")}) -----");
        sb.AppendLine("  ID  Jugador               Saldo  Casilla  Estado");

        string datos = p.Length > 3 ? p[3] : "";
        foreach (string fila in datos.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] f = fila.Split(',');
            if (f.Length < 5) continue;
            string estado = f[4] == "1" ? "activo" : "ELIMINADO";
            sb.AppendLine($"  {f[0],-3} {f[1],-20} {f[2],6}  {f[3],7}  {estado}");
        }
        return sb.ToString().TrimEnd();
    }

    private static string FormatearDuenios(string datos)
    {
        if (string.IsNullOrWhiteSpace(datos)) return "----- PROPIEDADES: ninguna ha sido comprada -----";

        var sb = new StringBuilder();
        sb.AppendLine("----- PROPIEDADES COMPRADAS (casilla, nombre, id del dueno) -----");
        foreach (string fila in datos.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] f = fila.Split(',');
            if (f.Length < 4) continue;
            sb.AppendLine($"  #{f[0],-3} {f[1],-20} dueno: jugador {f[2]}{(f[3] == "1" ? " (hipotecada)" : "")}");
        }
        return sb.ToString().TrimEnd();
    }
}