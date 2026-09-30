using System;
using MonopolyDistribuido;

namespace Monopoly;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length > 0)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "--server":
                    int puertoSrv = args.Length > 1 && int.TryParse(args[1], out int pS) ? pS : 5000;
                    var srvHw = new ControladorHardware();
                    var servidor = new Servidor(puertoSrv, srvHw);
                    servidor.Iniciar();
                    return;

                case "--client":
                    string ip = args.Length > 1 ? args[1] : "127.0.0.1";
                    int puertoCli = args.Length > 2 && int.TryParse(args[2], out int pC) ? pC : 5000;
                    string? nombre = args.Length > 3 ? args[3] : null;
                    Cliente.Conectar(ip, puertoCli, nombre);
                    return;

                case "--test":
                case "--demo":
                    PruebaIntegracion.EjecutarPruebaCompleta();
                    return;

                case "--unit-tests":
                    PruebasUnitarias.EjecutarTodas();
                    return;

                case "--rfid-test":
                    PruebaRfid.Ejecutar();
                    return;
            }
        }

        // Menú Principal Interactivo por consola
        while (true)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("        MONOPOLY DISTRIBUIDO - DATOS I           ");
            Console.WriteLine("=================================================");
            Console.WriteLine("  1. Iniciar Servidor (Autoridad central)");
            Console.WriteLine("  2. Iniciar Cliente (Conectar a partida)");
            Console.WriteLine("  3. Ejecutar Prueba Crítica de Integración (Demo)");
            Console.WriteLine("  4. Ejecutar Batería de Pruebas Unitarias");
            Console.WriteLine("  5. Diagnóstico de RFID / Hardware (PLUS)");
            Console.WriteLine("  6. Salir");
            Console.Write("Seleccione una opción: ");

            var opcion = Console.ReadLine()?.Trim();

            if (opcion == "1")
            {
                Console.Write("Puerto TCP [por defecto 5000]: ");
                string? inputPuerto = Console.ReadLine();
                int puerto = int.TryParse(inputPuerto, out int p) ? p : 5000;

                var hw = new ControladorHardware();
                var srv = new Servidor(puerto, hw);
                srv.Iniciar();
                break;
            }
            else if (opcion == "2")
            {
                Console.Write("IP del Servidor [por defecto 127.0.0.1]: ");
                string? ip = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

                Console.Write("Puerto TCP [por defecto 5000]: ");
                string? inputPuerto = Console.ReadLine();
                int puerto = int.TryParse(inputPuerto, out int p) ? p : 5000;

                Console.Write("Su Nombre de Jugador: ");
                string? nombre = Console.ReadLine();

                Cliente.Conectar(ip, puerto, nombre);
                break;
            }
            else if (opcion == "3")
            {
                PruebaIntegracion.EjecutarPruebaCompleta();
                Console.WriteLine("\nPresione ENTER para continuar...");
                Console.ReadLine();
            }
            else if (opcion == "4")
            {
                PruebasUnitarias.EjecutarTodas();
                Console.WriteLine("\nPresione ENTER para continuar...");
                Console.ReadLine();
            }
            else if (opcion == "5")
            {
                PruebaRfid.Ejecutar();
                Console.WriteLine("\nPresione ENTER para continuar...");
                Console.ReadLine();
            }
            else if (opcion == "6")
            {
                Console.WriteLine("Saliendo del programa.");
                break;
            }
        }
    }
}