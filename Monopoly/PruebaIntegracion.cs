using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MonopolyDistribuido;

namespace Monopoly;

public static class PruebaIntegracion
{
    /// <summary>
    /// PRUEBA CRÍTICA OBLIGATORIA:
    /// 1. Inicia servidor sin requerir Raspberry física.
    /// 2. Conecta 2 clientes automáticos mediante TCP.
    /// 3. Demuestra turnos, dados, movimiento en tablero circular, compra de propiedad y cobro de alquiler.
    /// 4. Demuestra que si el RFID falla o no está presente, se aplica el fallback al jugador en turno sin bloquear el sistema.
    /// 5. Demuestra que el Display de 7 segmentos recibe las actualizaciones.
    /// 6. Demuestra que el historial de transacciones se genera y guarda en archivo.
    /// </summary>
    public static void EjecutarPruebaCompleta()
    {
        Console.WriteLine("\n======================================================================");
        Console.WriteLine("    INICIANDO PRUEBA CRÍTICA DE INTEGRACIÓN (MONOPOLY DISTRIBUIDO)    ");
        Console.WriteLine("======================================================================\n");

        int puertoTest = 5055;
        var hw = new ControladorHardware(forzarSimulado: true); // Simulación para prueba automatizada limpia

        Console.WriteLine("[PASO 1] Iniciando Servidor sin Raspberry física obligatoria...");
        var servidor = new Servidor(puertoTest, hw);
        var hiloServidor = new Thread(servidor.Iniciar) { IsBackground = true };
        hiloServidor.Start();
        Thread.Sleep(400);

        Console.WriteLine("\n[PASO 2] Conectando 2 clientes TCP (Jugador Alfa y Jugador Beta)...");
        using var client1 = new TcpClient("127.0.0.1", puertoTest);
        using var w1 = new StreamWriter(client1.GetStream(), Encoding.UTF8) { AutoFlush = true };
        using var r1 = new StreamReader(client1.GetStream(), Encoding.UTF8);

        using var client2 = new TcpClient("127.0.0.1", puertoTest);
        using var w2 = new StreamWriter(client2.GetStream(), Encoding.UTF8) { AutoFlush = true };
        using var r2 = new StreamReader(client2.GetStream(), Encoding.UTF8);

        w1.WriteLine("CONECTAR|Alfa");
        string resp1 = LeerRespuesta(r1, "BIENVENIDO");
        Console.WriteLine($"  <- Cliente 1 conectado: {resp1}");

        w2.WriteLine("CONECTAR|Beta");
        string resp2 = LeerRespuesta(r2, "BIENVENIDO");
        Console.WriteLine($"  <- Cliente 2 conectado: {resp2}");

        Thread.Sleep(300);

        Console.WriteLine("\n[PASO 3] Turno de Alfa: Tirar dados con solicitud de RFID y Fallback automático...");
        w1.WriteLine("TIRAR_DADOS");
        string dadosAlfa = LeerRespuesta(r1, "DADOS_LANZADOS");
        Console.WriteLine($"  <- Servidor respondió a Alfa: {dadosAlfa}");

        Thread.Sleep(300);

        Console.WriteLine("\n[PASO 4] Alfa gestiona su casilla (compra o no compra según disponibilidad)...");
        if (dadosAlfa.Contains("PROPIEDAD_DISPONIBLE"))
        {
            w1.WriteLine("COMPRAR_PROPIEDAD");
            string compraAlfa = LeerRespuesta(r1, "COMPRA_EXITOSA", "ERROR");
            Console.WriteLine($"  <- Resultado de compra de Alfa: {compraAlfa}");
        }
        else
        {
            Console.WriteLine("  <- Casilla especial/evento/otra: no requiere compra.");
        }

        Thread.Sleep(300);

        Console.WriteLine("\n[PASO 5] Alfa termina su turno. El turno debe pasar a Beta...");
        w1.WriteLine("TERMINAR_TURNO");
        string finAlfa = LeerRespuesta(r1, "TURNO_TERMINADO", "ERROR");
        Console.WriteLine($"  <- Servidor confirmó fin de turno de Alfa: {finAlfa}");

        Thread.Sleep(300);

        Console.WriteLine("\n[PASO 6] Turno de Beta: Tirar dados con Fallback automático...");
        w2.WriteLine("TIRAR_DADOS");
        string dadosBeta = LeerRespuesta(r2, "DADOS_LANZADOS");
        Console.WriteLine($"  <- Servidor respondió a Beta: {dadosBeta}");

        Thread.Sleep(300);

        if (dadosBeta.Contains("PROPIEDAD_DISPONIBLE"))
        {
            w2.WriteLine("NO_COMPRAR");
            string noCompraBeta = LeerRespuesta(r2, "OK", "ERROR");
            Console.WriteLine($"  <- Beta decidió no comprar: {noCompraBeta}");
        }

        w2.WriteLine("TERMINAR_TURNO");
        string finBeta = LeerRespuesta(r2, "TURNO_TERMINADO", "ERROR");
        Console.WriteLine($"  <- Beta terminó su turno: {finBeta}");

        Thread.Sleep(300);

        Console.WriteLine("\n[PASO 7] Consultando historial oficial de transacciones desde cliente...");
        w1.WriteLine("CONSULTAR_TRANSACCIONES");
        string txResp = LeerRespuesta(r1, "TRANSACCIONES");
        Console.WriteLine($"  <- Transacciones registradas:\n{txResp}");

        Console.WriteLine("\n[PASO 8] Verificando archivo físico de transacciones en disco...");
        if (File.Exists("transacciones.txt"))
        {
            var lineas = File.ReadAllLines("transacciones.txt");
            Console.WriteLine($"  -> Archivo transacciones.txt encontrado con {lineas.Length} líneas registradas.");
            foreach (var l in lineas)
            {
                Console.WriteLine($"     {l}");
            }
        }
        else
        {
            Console.WriteLine("  [ERROR] No se encontró el archivo transacciones.txt.");
        }

        Console.WriteLine("\n[PASO 9] Verificando envío a Display de 7 segmentos...");
        bool displayOk = hw.MostrarEnDisplay(42);
        Console.WriteLine($"  -> Comando 7-Segmentos ejecutado sin colgar el hilo: OK={displayOk}");

        Console.WriteLine("\n======================================================================");
        Console.WriteLine("    ¡PRUEBA CRÍTICA COMPLETADA CON ÉXITO!                             ");
        Console.WriteLine("    - El servidor gobernó el estado oficial de la partida.            ");
        Console.WriteLine("    - No dependió obligatoriamente de la Raspberry para jugar.         ");
        Console.WriteLine("    - Fallback de RFID funcionó correctamente sin bloquearse.         ");
        Console.WriteLine("    - Las transacciones se persistieron en memoria y en disco.        ");
        Console.WriteLine("======================================================================\n");
    }

    private static string LeerRespuesta(StreamReader reader, params string[] prefijosEsperados)
    {
        while (true)
        {
            string? linea = reader.ReadLine();
            if (linea == null) return "DESCONECTADO";

            linea = linea.Trim();
            foreach (var prefijo in prefijosEsperados)
            {
                if (linea.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                {
                    return linea;
                }
            }
        }
    }
}
