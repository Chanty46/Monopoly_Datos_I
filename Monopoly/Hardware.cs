using System.IO.Ports;

namespace MonopolyDistribuido;

// El servidor solo conoce esta interfaz: puede usar la Pico real o la simulada.
public interface IDispositivoHardware
{
    // Muestra los dos dados en los 2 displays de 7 segmentos
    void MostrarDados(int d1, int d2);

    // Espera una tarjeta RFID. Devuelve el UID en mayusculas, o null si no se leyo nada.
    string? LeerRfid(int timeoutMs);
}

// ---------------------------------------------------------------------------
// Hardware SIMULADO (para probar sin la Pico).
// La "tarjeta" se acerca desde la consola del servidor con SimularTarjeta(uid).
// ---------------------------------------------------------------------------
public class ControladorHardware : IDispositivoHardware
{
    private string? uidPendiente;
    private readonly object candado = new();

    public void SimularTarjeta(string uid)
    {
        lock (candado) { uidPendiente = uid.Trim().ToUpperInvariant(); }
    }

    public void MostrarDados(int d1, int d2)
    {
        Console.WriteLine($"[HW SIMULADO] Display 7 segmentos: {d1} {d2}");
    }

    public string? LeerRfid(int timeoutMs)
    {
        DateTime limite = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < limite)
        {
            lock (candado)
            {
                if (uidPendiente != null)
                {
                    string uid = uidPendiente;
                    uidPendiente = null;
                    return uid;
                }
            }
            Thread.Sleep(100);
        }
        return null;
    }
}

// ---------------------------------------------------------------------------
// Hardware REAL: Raspberry Pi Pico por puerto serial USB (115200, comandos por linea).
// Comandos de la Pico: ISDARK, READID, LEDON, LEDOFF, DISPLAY <00-99>
// ---------------------------------------------------------------------------
public class HardwarePico : IDispositivoHardware, IDisposable
{
    private readonly SerialPort puerto;
    private readonly object candado = new();

    private HardwarePico(SerialPort puerto)
    {
        this.puerto = puerto;
    }

    // Abre la Pico. Si nombrePuerto es null se autodetecta (ignora ttyS* y COM1/COM2).
    public static HardwarePico Abrir(string? nombrePuerto = null)
    {
        if (nombrePuerto == null)
        {
            string[] candidatos = SerialPort.GetPortNames()
                .Where(p => !System.Text.RegularExpressions.Regex.IsMatch(
                    p, @"^(/dev/ttyS\d+|COM[12])$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                .ToArray();

            if (candidatos.Length == 0)
                throw new InvalidOperationException("No se encontro ningun puerto serial para la Pico.");

            nombrePuerto = candidatos[0];
        }

        var sp = new SerialPort(nombrePuerto, 115200)
        {
            ReadTimeout = 15000,   // un poco mas que los 10 s del READID
            NewLine = "\n"
        };
        sp.Open();
        Console.WriteLine($"[HW] Pico conectada en {nombrePuerto}");
        return new HardwarePico(sp);
    }

    private string? Enviar(string comando)
    {
        lock (candado)
        {
            try
            {
                puerto.WriteLine(comando);
                return puerto.ReadLine().Trim();
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"[HW] Sin respuesta de la Pico para '{comando}'.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HW] Error serial: {ex.Message}");
                return null;
            }
        }
    }

    public void MostrarDados(int d1, int d2)
    {
        // Un digito por display: DISPLAY 34 muestra 3 y 4
        Enviar($"DISPLAY {d1}{d2}");
    }

    // Se asume que READID responde con el UID (opcionalmente con prefijo "UID:")
    // o con un texto de error/timeout. Ajustar aqui si el firmware responde distinto.
    public string? LeerRfid(int timeoutMs)
    {
        string? resp = Enviar("READID");
        if (string.IsNullOrWhiteSpace(resp)) return null;

        string upper = resp.ToUpperInvariant();
        if (upper.StartsWith("UID:")) upper = upper.Substring(4).Trim();

        if (upper.Length == 0 || upper.Contains("TIMEOUT") || upper.Contains("ERROR") ||
            upper.Contains("NONE") || upper.Contains("NO_CARD"))
            return null;

        return upper;
    }

    public void Dispose()
    {
        try { puerto.Close(); } catch (Exception) { }
    }
}