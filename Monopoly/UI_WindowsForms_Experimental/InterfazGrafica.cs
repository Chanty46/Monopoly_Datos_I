namespace Monopoly;

// Única ventana del programa. Nunca se crean ni se cierran otras ventanas:
// lo que cambia es la "vista" (UserControl) que se muestra dentro del contenedor.
public class InterfazGrafica : Form
{
    // Panel que ocupa toda la ventana y donde se coloca la vista actual
    private readonly Panel _contenedor = new() { Dock = DockStyle.Fill };

    public InterfazGrafica()
    {
        Text = "Monopoly Distribuido";
        ClientSize = new Size(1000, 650);
        MinimumSize = new Size(800, 550);            // Evita que la ventana sea demasiado pequeña
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        Controls.Add(_contenedor);
        IrAMenu();                                   // Pantalla inicial
    }

    // Métodos de navegación: las vistas los llaman para cambiar de pantalla
    public void IrAMenu()          => Mostrar(new MenuPrincipal(this));
    public void IrATablero()       => Mostrar(new VistaTablero(this));
    public void IrAConfiguracion() => Mostrar(new VistaConfiguracion(this));

    // Reemplaza la vista actual por la nueva
    private void Mostrar(UserControl vista)
    {
        // 1. Quita y libera la vista anterior
        while (_contenedor.Controls.Count > 0)
        {
            var anterior = _contenedor.Controls[0];
            _contenedor.Controls.Remove(anterior);
            anterior.Dispose();
        }

        // 2. La nueva vista ocupa todo el contenedor
        vista.Dock = DockStyle.Fill;
        _contenedor.Controls.Add(vista);
    }
}