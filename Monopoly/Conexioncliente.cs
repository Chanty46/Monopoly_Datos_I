using System.Net.Sockets;
using System.Text;
using Monopoly;

namespace MonopolyDistribuido;

// Representa a un cliente conectado: su socket, su escritor (uno solo por cliente) y su jugador.
internal class ConexionCliente
{
    private readonly TcpClient socket;
    private readonly StreamWriter escritor;
    private readonly object candadoEscritura = new();

    public StreamReader Lector { get; }
    public string Endpoint { get; }

    public volatile bool Viva = true;
    public string Nombre = "";          // vacio = aun no ha enviado CONECTAR
    public Jugador? Jugador;            // se asigna al iniciar la partida
    public string? Uid;                 // UID de la tarjeta RFID asociada

    public ConexionCliente(TcpClient socket)
    {
        this.socket = socket;
        Endpoint = socket.Client.RemoteEndPoint?.ToString() ?? "desconocido";
        var stream = socket.GetStream();
        // UTF8 sin BOM: Encoding.UTF8 escribiria un BOM al inicio del stream
        var utf8 = new UTF8Encoding(false);
        Lector = new StreamReader(stream, utf8);
        escritor = new StreamWriter(stream, utf8) { AutoFlush = true, NewLine = "\n" };
    }

    // Envia una linea. Si falla, marca la conexion como caida (no lanza excepcion).
    public void Enviar(string linea)
    {
        if (!Viva) return;
        lock (candadoEscritura)
        {
            try { escritor.WriteLine(linea); }
            catch (Exception) { Viva = false; }
        }
    }

    public void Cerrar()
    {
        Viva = false;
        try { socket.Close(); } catch (Exception) { }
    }
}