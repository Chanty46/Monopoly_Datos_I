using MonopolyDistribuido;

namespace Monopoly;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "--server":
                    IniciarServidor(args);
                    return;
                case "--client":
                    string ip = args.Length > 1 ? args[1] : "127.0.0.1";
                    int puerto = args.Length > 2 && int.TryParse(args[2], out int pc) ? pc : 5000;
                    Cliente.Conectar(ip, puerto);
                    return;
                case "--local":
                case "--juego":
                    JuegoLocalPrueba.IniciarJuego();
                    return;
                case "--test":
                    PruebasUnitariasLogica.EjecutarPruebas();
                    return;
                case "--rfid-test":
                    PruebaRfid.Ejecutar();
                    return;
            }
        }

        Console.WriteLine("Monopoly");
        Console.WriteLine("1. Iniciar servidor (banco)");
        Console.WriteLine("2. Iniciar cliente");
        Console.WriteLine("3. Partida local de prueba");
        Console.WriteLine("4. Pruebas unitarias");
        Console.Write("Seleccione una opcion: ");

        switch (Console.ReadLine()?.Trim())
        {
            case "1": IniciarServidor(Array.Empty<string>()); break;
            case "2":
                Console.Write("IP del servidor [127.0.0.1]: ");
                string? entrada = Console.ReadLine();
                Cliente.Conectar(string.IsNullOrWhiteSpace(entrada) ? "127.0.0.1" : entrada.Trim(), 5000);
                break;
            case "3": JuegoLocalPrueba.IniciarJuego(); break;
            case "4": PruebasUnitariasLogica.EjecutarPruebas(); break;
            default: Console.WriteLine("Opcion invalida."); break;
        }
    }

    // Uso: --server [--puerto 5000] [--jugadores 4] [--turnos 100] [--rfid] [--pico [COMx|/dev/ttyACM0]]
    static void IniciarServidor(string[] args)
    {
        int puerto = ObtenerInt(args, "--puerto", 5000);
        int jugadores = ObtenerInt(args, "--jugadores", 4);
        int turnos = ObtenerInt(args, "--turnos", 100);
        bool exigirRfid = Array.IndexOf(args, "--rfid") >= 0;

        IDispositivoHardware hardware;
        int iPico = Array.IndexOf(args, "--pico");
        if (iPico >= 0)
        {
            string? nombrePuerto = iPico + 1 < args.Length && !args[iPico + 1].StartsWith("--") ? args[iPico + 1] : null;
            try
            {
                hardware = HardwarePico.Abrir(nombrePuerto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HW] No se pudo abrir la Pico ({ex.Message}). Se usa hardware simulado.");
                hardware = new ControladorHardware();
            }
        }
        else
        {
            hardware = new ControladorHardware();
        }

        new Servidor(puerto, hardware, jugadores, turnos, exigirRfid).Iniciar();
    }

    static int ObtenerInt(string[] args, string bandera, int defecto)
    {
        int i = Array.IndexOf(args, bandera);
        return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int v) ? v : defecto;
    }
}