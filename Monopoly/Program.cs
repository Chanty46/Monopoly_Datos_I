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
    }
}