
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