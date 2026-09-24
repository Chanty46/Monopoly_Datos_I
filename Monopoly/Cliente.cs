using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MonopolyDistribuido
{
    public class Cliente
    {
        public static void Conectar(string ip, int puerto)
        {
            try {
                using var socket = new TcpClient(ip, puerto);
                using var stream = socket.GetStream();
                using var r = new StreamReader(stream, Encoding.UTF8);
                using var w = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                Console.WriteLine("[CLIENTE] Conectado al servidor.");

                new Thread(() => {
                    while (socket.Connected) {
                        try {
                            var msg = r.ReadLine();
                            if (msg != null) Console.WriteLine($"\n[RED]-> {msg}\nOpcion> ");
                        } catch { break; }
                    }
                }) { IsBackground = true }.Start();

                while (socket.Connected) {
                    Console.WriteLine("\n--- ACCIONES CANAL ---");
                    Console.WriteLine("1. Solicitud Tirar Dados (Pico)");
                    Console.WriteLine("2. Solicitud Leer RFID (Pico)");
                    Console.WriteLine("3. Salir");
                    Console.Write("Opcion> ");
                    
                    var op = Console.ReadLine();
                    if (op == "1") w.WriteLine("TIRAR_DADOS");
                    else if (op == "2") w.WriteLine("READID");
                    else if (op == "3") break;
                }
            } catch (Exception ex) {
                Console.WriteLine($"[CLIENTE ERROR]: {ex.Message}");
            }
        }
    }
}