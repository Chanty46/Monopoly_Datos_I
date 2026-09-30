using System;
using MonopolyDistribuido;

namespace Monopoly;

public static class PruebaRfid
{
    public static void Ejecutar()
    {
        Console.WriteLine("\n[RFID TEST] Iniciando prueba de lectura de tarjeta física/simulada...");

        var controlador = new ControladorHardware();
        Console.WriteLine($"[RFID TEST] Estado del hardware: {controlador.Estado} en puerto {controlador.PuertoActual}");
        Console.WriteLine("[RFID TEST] Esperando tarjeta RFID durante 5 segundos...");

        var uid = controlador.SolicitarRfid(5000);

        if (!string.IsNullOrWhiteSpace(uid))
        {
            Console.WriteLine($"[RFID TEST] ÉXITO: Tarjeta detectada con UID: {uid}");
        }
        else
        {
            Console.WriteLine("[RFID TEST] AVISO: No se detectó ninguna tarjeta (o hardware no conectado).");
            Console.WriteLine("[RFID TEST] En el juego oficial, esto activa el FALLBACK AUTOMÁTICO al jugador en turno sin bloquear.");
        }
    }
}
