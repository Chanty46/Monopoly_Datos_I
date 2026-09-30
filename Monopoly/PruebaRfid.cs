// Comandos disponibles: ISDARK, READID, LEDON, LEDOFF, DISPLAY <00-99>, SALIR
//
// Nota sobre baudrate: la Pico se conecta como puerto serial USB (CDC-ACM),
// por lo que el valor de baudrate no afecta la velocidad real de transferencia
// (la maneja el propio USB). Cualquier valor funciona, EXCEPTO 2400, que puede
// forzar a la Pico a entrar en modo BOOTSEL. Se deja 115200 por convencion.
//
// Deteccion de puerto: en vez de asumir un nombre fijo (COM3, /dev/ttyACM0, etc.),
// se usa SerialPort.GetPortNames() para listar los puertos disponibles en el
// sistema operativo actual (Windows, Linux o macOS) y se elige automaticamente
// si hay uno solo, o se le pide al usuario que elija si hay varios.

using System;
using System.IO.Ports;
using System.Linq;

namespace MonopolyDistribuido;

public static class PruebaRfid
{
    public static void Ejecutar()
    {
        SerialPort? puerto = null;

        // Se reintenta la conexion si falla (por ejemplo, si el usuario
        // todavia no conecto la Pico o si el puerto elegido no responde).
        while (puerto == null)
        {
            string? nombrePuerto = SeleccionarPuerto();

            if (nombrePuerto == null)
            {
                Console.Write("Intentar de nuevo? (S/N): ");
                string? reintentar = Console.ReadLine();
                if (reintentar?.Trim().ToUpper() != "S")
                {
                    return;
                }
                continue;
            }

            try
            {
                puerto = new SerialPort(nombrePuerto, 115200)
                {
                    ReadTimeout = 15000, // un poco mas que los 10s del READID
                    NewLine = "\n"
                };
                puerto.Open();
                Console.WriteLine($"Conectado a {nombrePuerto}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo abrir el puerto {nombrePuerto}: {ex.Message}");

                if (!OperatingSystem.IsWindows())
                {
                    string detalle = ex.Message.ToLowerInvariant();

                    if (detalle.Contains("denied") || detalle.Contains("permission"))
                    {
                        Console.WriteLine("Probablemente el usuario no tiene permisos sobre el dispositivo serial.");
                        Console.WriteLine("Revise las instrucciones para Linux (grupo dialout/uucp) en el instructivo.");
                    }
                    else if (detalle.Contains("busy") || detalle.Contains("in use") || detalle.Contains("sharing"))
                    {
                        Console.WriteLine("El puerto parece estar en uso por otro programa (por ejemplo, Thonny).");
                        Console.WriteLine("Cierre ese programa y vuelva a intentar.");
                    }
                }

                puerto = null;
                Console.Write("Intentar de nuevo? (S/N): ");
                string? reintentar = Console.ReadLine();
                if (reintentar?.Trim().ToUpper() != "S")
                {
                    return;
                }
            }
        }

        Console.WriteLine("Comandos: ISDARK, READID, LEDON, LEDOFF, DISPLAY <00-99>, SALIR");

        while (true)
        {
            Console.Write("> ");
            string? comando = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(comando))
            {
                continue;
            }

            comando = comando.Trim().ToUpper();

            if (comando == "SALIR")
            {
                break;
            }

            try
            {
                puerto.WriteLine(comando);
                string respuesta = puerto.ReadLine();
                Console.WriteLine("Respuesta: " + respuesta.Trim());
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Sin respuesta de la Pico (timeout).");
            }
        }

        puerto.Close();
        Console.WriteLine("Conexion cerrada.");
    }

    // Detecta los puertos seriales disponibles en el sistema operativo actual
    // (Windows: COMx, Linux: /dev/ttyACMx o /dev/ttyUSBx, macOS: /dev/tty.usbmodemXXXX)
    // y deja elegir al usuario si hay mas de uno. Nunca asume un nombre fijo.
    static string? SeleccionarPuerto()
    {
        string[] puertos = SerialPort.GetPortNames();

        if (puertos.Length == 0)
        {
            Console.WriteLine("No se encontro ningun puerto serial.");
            Console.WriteLine("Conecte la Raspberry Pi Pico por USB y vuelva a intentar.");
            return null;
        }

        // En Linux, GetPortNames() casi siempre incluye tambien los puertos
        // seriales "fantasma" de la placa base (/dev/ttyS0, /dev/ttyS1, ...),
        // que existen aunque no haya ningun dispositivo real conectado. Si no
        // se filtran, en Linux casi nunca se detectaria "un solo puerto" (el
        // caso de seleccion automatica), aunque la Pico sea el unico
        // dispositivo USB conectado. Se ocultan de la lista principal, pero
        // sin perder la opcion de verlos: si tras filtrar no queda ningun
        // puerto "candidato", se usa la lista completa como respaldo.
        string[] candidatos = puertos.Where(p => !EsPuertoSerialLegado(p)).ToArray();
        if (candidatos.Length == 0)
        {
            candidatos = puertos;
        }

        if (candidatos.Length == 1)
        {
            Console.WriteLine($"Puerto detectado automaticamente: {candidatos[0]}");
            return candidatos[0];
        }

        Console.WriteLine("Se encontraron varios puertos seriales:");
        for (int i = 0; i < candidatos.Length; i++)
        {
            Console.WriteLine($"  [{i}] {candidatos[i]}");
        }

        Console.Write("Elegir el numero del puerto donde esta la Pico: ");
        string? entrada = Console.ReadLine();

        if (int.TryParse(entrada, out int indice) && indice >= 0 && indice < candidatos.Length)
        {
            return candidatos[indice];
        }

        Console.WriteLine("Opcion invalida.");
        return null;
    }

    // Puertos seriales integrados de placa (COM1/COM2 en Windows, ttySx en
    // Linux) que casi nunca corresponden a un dispositivo USB como la Pico.
    // Es solo una ayuda para no mostrarlos primero; nunca se descarta un
    // puerto real "de verdad" porque siempre queda el respaldo con la lista
    // completa si el filtro deja la lista vacia.
    static bool EsPuertoSerialLegado(string nombrePuerto)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            nombrePuerto, @"^(/dev/ttyS\d+|COM[12])$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}