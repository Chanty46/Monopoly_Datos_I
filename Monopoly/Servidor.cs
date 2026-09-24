using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MonopolyDistribuido
{
    public class Servidor
    {
        private TcpListener listener;
        private ControladorHardware hw;
        private List<TcpClient> clientes = new();
        private bool corriendo = true;

        public Servidor(int puertoTcp, ControladorHardware hardware)
        {
            hw = hardware;
            listener = new TcpListener(IPAddress.Any, puertoTcp);
        }

        public void Iniciar()
        {
            listener.Start();
            Console.WriteLine("[SERVIDOR] Escuchando conexiones TCP...");
            
            new Thread(AceptarClientes) { IsBackground = true }.Start();

            while (corriendo) {
                Console.WriteLine("\n[MENU SERVIDOR] 1. Probar Dados Pico | 2. Probar RFID | 3. Salir");
                var op = Console.ReadLine();
                if (op == "1" && hw != null) Console.WriteLine("Pico: " + hw.EnviarComando("TIRAR_DADOS"));
                else if (op == "2" && hw != null) Console.WriteLine("Pico: " + hw.EnviarComando("READID"));
                else if (op == "3") { corriendo = false; listener.Stop(); }
            }
        }

        private void AceptarClientes()
        {
            while (corriendo) {
                try {
                    var socket = listener.AcceptTcpClient();
                    lock (clientes) clientes.Add(socket);
                    Console.WriteLine("[SERVIDOR] Nuevo cliente conectado.");
                    new Thread(() => AtenderCliente(socket)) { IsBackground = true }.Start();
                } catch { break; }
            }
        }

        private void AtenderCliente(TcpClient client)
        {
            using var stream = client.GetStream();
            using var r = new StreamReader(stream, Encoding.UTF8);
            
            while (corriendo && client.Connected) {
                var msg = r.ReadLine();
                if (msg == null) break;
                Console.WriteLine($"[RECIBIDO CLIENTE]: {msg}");

                string respuesta = "OK";
                if (msg == "TIRAR_DADOS")
                {
                    respuesta = hw != null ? hw.EnviarComando("TIRAR_DADOS") : "DADOS:3,4";
                    Console.WriteLine($"[SERVIDOR] Estado actualizado: {respuesta}");
                }
                else if (msg == "READID")
                {
                    respuesta = hw != null ? hw.EnviarComando("READID") : "UID:SIMULADO";
                }

                TransmitirATodos($"RESPUESTA|{msg}|{respuesta}");
            }

            lock (clientes) clientes.Remove(client);
            client.Close();
        }

        private void TransmitirATodos(string mensaje)
        {
            lock (clientes) {
                foreach (var c in clientes) {
                    if (c.Connected) {
                        try {
                            var w = new StreamWriter(c.GetStream(), Encoding.UTF8) { AutoFlush = true };
                            w.WriteLine(mensaje);
                        } catch { }
                    }
                }
            }
        }
    }
}