namespace Monopoly;

class Program
{
    static void Main(string[] args)
    {
        // Si se pasa el argumento --local o --juego, inicia directo la partida
        if (args.Length > 0 && (args[0] == "--local" || args[0] == "--juego"))
        {
            JuegoLocalPrueba.IniciarJuego();
            return;
        }

        // Si se pasa --test, corre las pruebas directamente
        if (args.Length > 0 && args[0] == "--test")
        {
            PruebasUnitariasLogica.EjecutarPruebas();
            return;
        }

        // Por defecto ejecuta las pruebas unitarias y ofrece iniciar el juego local
        PruebasUnitariasLogica.EjecutarPruebas();

        Console.WriteLine("\n¿Desea iniciar una partida local de prueba interactiva? (S/N): ");
        string? resp = Console.ReadLine();
        if (resp != null && (resp.Trim().ToUpper() == "S" || resp.Trim().ToUpper() == "SI"))
        {
            JuegoLocalPrueba.IniciarJuego();
        }
    }
}
