using MonopolyDistribuido;

namespace Monopoly;

public static class PruebaRfid
{
    public static void Ejecutar()
    {
        Console.WriteLine("[RFID TEST] Iniciando prueba de lectura de tarjeta...");

        var controlador = new ControladorHardware();
        var respuesta = controlador.EnviarComando("READID");

        Console.WriteLine($"[RFID TEST] Respuesta: {respuesta}");

        var ok = !string.IsNullOrWhiteSpace(respuesta) &&
                 respuesta.StartsWith("UID:", StringComparison.OrdinalIgnoreCase);

        if (ok)
        {
            Console.WriteLine("[RFID TEST] OK: el valor devuelto tiene formato de UID válido.");
        }
        else
        {
            Console.WriteLine("[RFID TEST] ERROR: la respuesta no tiene el formato esperado (UID:...).");
        }
    }
}
