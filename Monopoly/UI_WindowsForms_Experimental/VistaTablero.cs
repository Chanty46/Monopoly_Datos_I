namespace Monopoly;

// Pantalla temporal. Aquí se dibujará el tablero de 24 casillas.
// Esta clase solo MUESTRA lo que diga el servidor: no debe calcular saldos, turnos ni posiciones.
public class VistaTablero : UserControl
{
    public VistaTablero(InterfazGrafica ventana)
    {
        BackColor = Color.White;

        var lbl = new Label
        {
            Text = "Aquí irá el tablero",
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

        // Con Dock: primero el Fill, luego el Bottom
        Controls.Add(lbl);
        Controls.Add(btnVolver);
    }
}