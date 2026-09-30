namespace Monopoly;

// Pantalla del menú inicial. Es un UserControl: vive dentro de la ventana, no es otra ventana.
public class MenuPrincipal : UserControl
{
    public MenuPrincipal(InterfazGrafica ventana)
    {
        BackColor = Color.White;

        var titulo = new Label
        {
            Text = "MONOPOLY",
            Font = new Font("Segoe UI", 36, FontStyle.Bold),
            AutoSize = true
        };
        var btnJugar = CrearBoton("Jugar");
        var btnConfig = CrearBoton("Configuración");

        // Al hacer clic, se le pide a la ventana principal cambiar de vista
        btnJugar.Click += (s, e) => ventana.IrATablero();
        btnConfig.Click += (s, e) => ventana.IrAConfiguracion();

        Controls.AddRange(new Control[] { titulo, btnJugar, btnConfig });

        // Mantiene todo centrado aunque se cambie el tamaño de la ventana
        Load += (s, e) => Centrar(titulo, btnJugar, btnConfig);
        Resize += (s, e) => Centrar(titulo, btnJugar, btnConfig);
    }

    private static Button CrearBoton(string texto) => new()
    {
        Text = texto,
        Size = new Size(240, 55),
        Font = new Font("Segoe UI", 14),
        FlatStyle = FlatStyle.System
    };

    private void Centrar(Label titulo, Button jugar, Button config)
    {
        int cx = Width / 2;
        int y = Height / 2 - 150;

        titulo.Location = new Point(cx - titulo.Width / 2, y);
        jugar.Location = new Point(cx - jugar.Width / 2, y + 120);
        config.Location = new Point(cx - config.Width / 2, y + 195);
    }
}