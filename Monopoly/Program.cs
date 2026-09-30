namespace Monopoly;

class Program
{
    [STAThread] // Requerido por Windows Forms (agregar)
    static void Main()
    {
        // ---- Código de tus compañeros (no se toca) ----
        Casilla c = new Casilla("Holaaa", 123);
        Console.WriteLine("Casilla creada correctamente, todo se está leyendo bien.");

        // ---- Interfaz gráfica (agregar) ----
        ApplicationConfiguration.Initialize();   // Configura estilos visuales y DPI
        Application.Run(new InterfazGrafica());  // Abre la única ventana del juego
﻿
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
                    var servidor = new Servidor(5000, new ControladorHardware());
                    servidor.Iniciar();
                    return;
                case "--client":
                    var ip = args.Length > 1 ? args[1] : "127.0.0.1";
                    var puerto = args.Length > 2 ? int.Parse(args[2]) : 5000;
                    Cliente.Conectar(ip, puerto);
                    return;
                case "--rfid-test":
                    PruebaRfid.Ejecutar();
                    return;
            }
        }

        Console.WriteLine("Monopoly");
        Console.WriteLine("1. Iniciar servidor");
        Console.WriteLine("2. Iniciar cliente");
        Console.Write("Seleccione una opción: ");

        var opcion = Console.ReadLine();

        if (opcion == "1")
        {
            var servidor = new Servidor(5000, new ControladorHardware());
            servidor.Iniciar();
        }
        else if (opcion == "2")
        {
            Cliente.Conectar("127.0.0.1", 5000);
        }
        else
        {
            Casilla c = new Casilla("Holaaa", 123);
            Console.WriteLine($"Casilla creada correctamente: {c.getNombre()} ({c.getIdCasilla()})");
        }
    }
}