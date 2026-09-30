namespace Monopoly;

// Pantalla temporal. Luego llevará IP del servidor, nombre del jugador, máximo de turnos, etc.
public class VistaConfiguracion : UserControl
{
    public VistaConfiguracion(InterfazGrafica ventana)
    {
        BackColor = Color.White;

        var lbl = new Label
        {
            Text = "Configuración",
            Font = new Font("Segoe UI", 20),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var btnVolver = new Button
        {
            Text = "Volver al menú",
            Dock = DockStyle.Bottom,
            Height = 50
        };
        btnVolver.Click += (s, e) => ventana.IrAMenu();

        Controls.Add(lbl);
        Controls.Add(btnVolver);
    }
}