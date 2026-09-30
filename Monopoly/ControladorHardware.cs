using System;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace MonopolyDistribuido;

public enum EstadoHardware
{
    CONECTADA,
    DESCONECTADA,
    NO_DISPONIBLE,
    SIMULADA
}

public class ControladorHardware
{
    private SerialPort? _puerto;
    private readonly object _lockPuerto = new();
    private readonly Random _random = new();
    private bool _intentarReconexion = true;
    private Thread? _hiloReconexion;

    public EstadoHardware Estado { get; private set; } = EstadoHardware.DESCONECTADA;
    public string PuertoActual { get; private set; } = "NINGUNO";
    public bool ModoSimuladoForzado { get; set; } = false;

    public ControladorHardware(bool forzarSimulado = false)
    {
        ModoSimuladoForzado = forzarSimulado;
        if (forzarSimulado)
        {
            Estado = EstadoHardware.SIMULADA;
            Console.WriteLine("[HARDWARE] Modo simulado activado por configuración.");
            return;
        }

        // Intento inicial no bloqueante
        IntentarConectar();

        // Iniciar hilo de reconexión periódica en segundo plano
        _hiloReconexion = new Thread(CicloReconexionSegundoPlano)
        {
            IsBackground = true,
            Name = "HardwareReconnectThread"
        };
        _hiloReconexion.Start();
    }

    private void CicloReconexionSegundoPlano()
    {
        while (_intentarReconexion)
        {
            Thread.Sleep(5000); // Revisar cada 5 segundos

            if (ModoSimuladoForzado) continue;

            lock (_lockPuerto)
            {
                if (Estado != EstadoHardware.CONECTADA)
                {
                    IntentarConectar();
                }
            }
        }
    }

    private void IntentarConectar()
    {
        try
        {
            string? candidato = DetectarPuertoPico();
            if (candidato == null)
            {
                if (Estado != EstadoHardware.DESCONECTADA)
                {
                    Estado = EstadoHardware.DESCONECTADA;
                    Console.WriteLine("[HARDWARE] Raspberry Pi Pico: DESCONECTADA (sin puertos seriales válidos). El juego continúa normalmente.");
                }
                return;
            }

            // Si ya teníamos un puerto abierto con el mismo nombre y está funcionando, no reabrir
            if (_puerto != null && _puerto.IsOpen && PuertoActual == candidato)
            {
                return;
            }

            CerrarPuertoSilencioso();

            _puerto = new SerialPort(candidato, 115200)
            {
                ReadTimeout = 3000,
                WriteTimeout = 1500,
                NewLine = "\n"
            };

            _puerto.Open();
            Estado = EstadoHardware.CONECTADA;
            PuertoActual = candidato;
            Console.WriteLine($"[HARDWARE] Raspberry Pi Pico CONECTADA en puerto {candidato}.");

            // Enviar un LEDON y luego LEDOFF breve como confirmación visual
            try
            {
                _puerto.DiscardInBuffer();
                _puerto.WriteLine("LEDON");
                Thread.Sleep(100);
                try { _puerto.ReadLine(); } catch { }
                _puerto.WriteLine("LEDOFF");
                Thread.Sleep(100);
                try { _puerto.ReadLine(); } catch { }
                _puerto.DiscardInBuffer();
            }
            catch { }
        }
        catch (Exception ex)
        {
            Estado = EstadoHardware.NO_DISPONIBLE;
            PuertoActual = "ERROR";
            Console.WriteLine($"[HARDWARE WARNING] No se pudo acceder a la Pico: {ex.Message}. Operando en modo degradado.");
            CerrarPuertoSilencioso();
        }
    }

    private void CerrarPuertoSilencioso()
    {
        try
        {
            if (_puerto != null)
            {
                if (_puerto.IsOpen) _puerto.Close();
                _puerto.Dispose();
                _puerto = null;
            }
        }
        catch { }
    }

    private string? DetectarPuertoPico()
    {
        try
        {
            string[] puertos = SerialPort.GetPortNames();
            if (puertos.Length == 0) return null;

            // Filtrar puertos heredados en Linux (/dev/ttyS*) y Windows (COM1/COM2)
            string[] candidatos = puertos.Where(p => !EsPuertoSerialLegado(p)).ToArray();
            if (candidatos.Length == 0)
            {
                candidatos = puertos;
            }

            return candidatos.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static bool EsPuertoSerialLegado(string nombrePuerto)
    {
        return Regex.IsMatch(nombrePuerto, @"^(/dev/ttyS\d+|COM[12])$", RegexOptions.IgnoreCase);
    }

    // =========================================================================
    // COMANDOS DEL HARDWARE CON FALLBACK TOTALMENTE NO BLOQUEANTE
    // =========================================================================

    /// <summary>
    /// Intenta leer RFID en la Pico. Si no hay hardware, hay timeout o la tarjeta no es detectada,
    /// retorna null inmediatamente después del timeout sin bloquear el servidor.
    /// </summary>
    public string? SolicitarRfid(int timeoutMs = 2500)
    {
        if (ModoSimuladoForzado || Estado != EstadoHardware.CONECTADA || _puerto == null)
        {
            Console.WriteLine("[HARDWARE] Solicitud RFID omitida: hardware no conectado. Utilizando jugador en turno.");
            return null;
        }

        lock (_lockPuerto)
        {
            try
            {
                if (!_puerto.IsOpen)
                {
                    Estado = EstadoHardware.DESCONECTADA;
                    return null;
                }

                _puerto.DiscardInBuffer();
                _puerto.ReadTimeout = timeoutMs;
                _puerto.WriteLine("READID");

                string respuesta = _puerto.ReadLine().Trim();
                if (!string.IsNullOrWhiteSpace(respuesta) && respuesta != "TIMEOUT" && !respuesta.StartsWith("ERROR") && respuesta != "OK")
                {
                    // Validar formato hexadecimal real (4 a 16 caracteres hexadecimales)
                    if (Regex.IsMatch(respuesta, @"^[0-9A-Fa-f]{4,16}$"))
                    {
                        string uidNormalizado = respuesta.ToUpperInvariant();
                        Console.WriteLine($"[HARDWARE] RFID leído exitosamente: {uidNormalizado}");
                        return uidNormalizado;
                    }
                }

                Console.WriteLine("[HARDWARE] RFID: Timeout o sin tarjeta detectada. Se aplicará fallback al jugador actual.");
                return null;
            }
            catch (TimeoutException)
            {
                Console.WriteLine("[HARDWARE] RFID: Timeout agotado esperando tarjeta. Continuando con jugador actual.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HARDWARE WARNING] Error en lectura RFID: {ex.Message}. Desconectando puerto.");
                Estado = EstadoHardware.DESCONECTADA;
                CerrarPuertoSilencioso();
                return null;
            }
        }
    }

    /// <summary>
    /// Envía un valor de 2 dígitos (00 a 99) al Display de 7 Segmentos multiplexado.
    public bool MostrarDados(int d1, int d2)
    {
        int valorDisplay = (Math.Clamp(d1, 1, 9) * 10) + Math.Clamp(d2, 1, 9);
        return MostrarEnDisplay(valorDisplay);
    }

    /// <summary>
    /// Envía un valor de 2 dígitos (00 a 99) al Display de 7 Segmentos multiplexado.
    /// SIEMPRE se intenta enviar si hay conexión física. Si no hay conexión o falla,
    /// solo se registra en log y NUNCA bloquea el servidor.
    /// </summary>
    public bool MostrarEnDisplay(int valor)
    {
        int valorAjustado = Math.Clamp(valor, 0, 99);
        string comando = $"DISPLAY {valorAjustado:D2}";

        Console.WriteLine($"[7-SEGMENTOS] Actualizando display físico a valor: '{valorAjustado:D2}'");

        if (ModoSimuladoForzado || Estado != EstadoHardware.CONECTADA || _puerto == null)
        {
            return false;
        }

        lock (_lockPuerto)
        {
            try
            {
                if (!_puerto.IsOpen)
                {
                    Estado = EstadoHardware.DESCONECTADA;
                    return false;
                }

                _puerto.DiscardInBuffer();
                _puerto.WriteLine(comando);
                string resp = _puerto.ReadLine().Trim();
                return resp == "OK";
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"[HARDWARE WARNING] Timeout temporal esperando confirmación del display.");
                // No desconectar el puerto inmediatamente por un timeout temporal; mantener conexión activa
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HARDWARE WARNING] No se pudo enviar datos al display: {ex.Message}");
                Estado = EstadoHardware.DESCONECTADA;
                CerrarPuertoSilencioso();
                return false;
            }
        }
    }

    /// <summary>
    /// Genera la tirada oficial de dados. Si la Raspberry Pi Pico está conectada
    /// y responde con dados electrónicos, utiliza sus valores. De lo contrario,
    /// aplica fallback inmediato y seguro a la generación aleatoria del servidor.
    /// Los 2 displays de 7 segmentos muestran cada dado (ej: 3 y 4 se muestran como '34').
    /// </summary>
    public (int d1, int d2, int suma) TirarDados()
    {
        if (!ModoSimuladoForzado && Estado == EstadoHardware.CONECTADA && _puerto != null)
        {
            lock (_lockPuerto)
            {
                try
                {
                    if (_puerto.IsOpen)
                    {
                        _puerto.ReadTimeout = 1000;
                        _puerto.WriteLine("TIRAR_DADOS");
                        string respuesta = _puerto.ReadLine().Trim();
                        if (respuesta.StartsWith("DADOS:", StringComparison.OrdinalIgnoreCase))
                        {
                            var partes = respuesta.Substring(6).Split(',');
                            if (partes.Length == 2 && int.TryParse(partes[0], out int pD1) && int.TryParse(partes[1], out int pD2))
                            {
                                int pTotal = pD1 + pD2;
                                Console.WriteLine($"[HARDWARE DADOS] Dados electrónicos recibidos de la Pico: Dado1={pD1}, Dado2={pD2} (Total={pTotal})");
                                MostrarDados(pD1, pD2);
                                return (pD1, pD2, pTotal);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        int d1 = _random.Next(1, 7);
        int d2 = _random.Next(1, 7);
        int total = d1 + d2;

        Console.WriteLine($"[DADOS] Resultado generado: Dado1={d1}, Dado2={d2} (Total={total})");
        MostrarDados(d1, d2);

        return (d1, d2, total);
    }

    public void EncenderLed(bool encender)
    {
        if (Estado != EstadoHardware.CONECTADA || _puerto == null) return;

        lock (_lockPuerto)
        {
            try
            {
                if (_puerto.IsOpen)
                {
                    _puerto.WriteLine(encender ? "LEDON" : "LEDOFF");
                    _puerto.ReadLine();
                }
            }
            catch { }
        }
    }

    public void Detener()
    {
        _intentarReconexion = false;
        CerrarPuertoSilencioso();
    }
}
