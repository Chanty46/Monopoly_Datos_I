using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Monopoly;

namespace MonopolyDistribuido;

// ============================================================================
// SERVIDOR / BANCO
// Mantiene el estado OFICIAL de la partida. Los clientes solo envian solicitudes;
// el servidor las valida, modifica el estado (a traves de Banco), registra las
// transacciones y notifica a todos los clientes.
//
// Concurrencia: hay un hilo por cliente. TODA operacion que lee o modifica el
// estado del juego se hace dentro de lock (estado). Orden de locks (nunca al reves):
//   estado -> lista de conexiones -> escritura de cada conexion
// ============================================================================
public class Servidor
{
    private const int SaldoInicial = 1500;
    private const int TimeoutRfidMs = 10000;
    private static readonly Regex NombreValido = new(@"^[\p{L}\p{N} _\-\.]{1,20}$");

    private readonly TcpListener listener;
    private readonly IDispositivoHardware? hw;
    private readonly ListaConexiones conexiones = new();
    private readonly object estado = new();
    private readonly int jugadoresRequeridos;
    private readonly int maxTurnos;
    private readonly bool exigirRfid;
    private volatile bool corriendo = true;

    // ---- Estado de la partida (protegido por lock(estado)) ----
    private Banco? banco;
    private readonly ListaDobleTransacciones historial = new();
    private bool partidaIniciada;
    private bool partidaFinalizada;
    private int numeroTurno = 1;     // contador global de turnos jugados
    private bool yaTiroDados;        // el jugador en turno ya lanzo los dados
    private bool puedeComprar;       // el jugador en turno puede comprar la casilla donde cayo

    public Servidor(int puertoTcp, IDispositivoHardware? hardware = null,
                    int jugadoresRequeridos = 4, int maxTurnos = 100, bool exigirRfid = false)
    {
        hw = hardware;
        this.jugadoresRequeridos = Math.Clamp(jugadoresRequeridos, 2, 4);
        this.maxTurnos = Math.Max(1, maxTurnos);
        this.exigirRfid = exigirRfid && hardware != null;
        listener = new TcpListener(IPAddress.Any, puertoTcp);
    }

    // ========================================================================
    // ARRANQUE Y MENU DE CONSOLA DEL SERVIDOR
    // ========================================================================
    public void Iniciar()
    {
        listener.Start();
        Log($"Escuchando en el puerto {((IPEndPoint)listener.LocalEndpoint).Port}. " +
            $"Jugadores requeridos: {jugadoresRequeridos}. Turnos maximos: {maxTurnos}. " +
            $"RFID obligatorio en compras: {(exigirRfid ? "SI" : "NO")}.");

        new Thread(AceptarClientes) { IsBackground = true }.Start();
        MenuServidor();
        Detener();
    }

    private void MenuServidor()
    {
        while (corriendo)
        {
            Console.WriteLine("\n[MENU SERVIDOR]");
            Console.WriteLine(" 1. Iniciar partida ahora (con los jugadores conectados)");
            Console.WriteLine(" 2. Ver estado");
            Console.WriteLine(" 3. Ver transacciones");
            Console.WriteLine(" 4. Exportar transacciones a TXT");
            Console.WriteLine(" 5. Probar display de dados (hardware)");
            Console.WriteLine(" 6. Simular tarjeta RFID (solo hardware simulado)");
            Console.WriteLine(" 0. Salir");
            Console.Write("Opcion> ");

            string? op = Console.ReadLine();
            if (op == null) break;

            switch (op.Trim())
            {
                case "1":
                    lock (estado)
                    {
                        if (partidaIniciada) Console.WriteLine("La partida ya inicio.");
                        else if (conexiones.ContarRegistrados() < 2) Console.WriteLine("Se necesitan al menos 2 jugadores.");
                        else IniciarPartida();
                    }
                    break;
                case "2":
                    lock (estado)
                    {
                        if (banco == null) Console.WriteLine($"Lobby: {conexiones.ContarRegistrados()}/{jugadoresRequeridos} jugadores.");
                        else Console.WriteLine(banco.ObtenerEstado());
                    }
                    break;
                case "3":
                    lock (estado) { historial.ImprimirTodas(); }
                    break;
                case "4":
                    lock (estado) { ExportarHistorial(); }
                    break;
                case "5":
                    if (hw == null) { Console.WriteLine("No hay hardware configurado."); break; }
                    var rnd = new Random();
                    hw.MostrarDados(rnd.Next(1, 7), rnd.Next(1, 7));
                    break;
                case "6":
                    if (hw is ControladorHardware sim)
                    {
                        Console.Write("UID de la tarjeta a acercar (ej. AB12CD34): ");
                        string? uid = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(uid)) { sim.SimularTarjeta(uid); Console.WriteLine("Tarjeta acercada al lector simulado."); }
                    }
                    else Console.WriteLine("Solo disponible con el hardware simulado.");
                    break;
                case "0":
                    corriendo = false;
                    break;
            }
        }
    }

    private void Detener()
    {
        corriendo = false;
        try { listener.Stop(); } catch (Exception) { }
        conexiones.Recorrer(c => c.Cerrar());
        Log("Servidor detenido.");
    }

    // ========================================================================
    // RED: ACEPTAR Y ATENDER CLIENTES
    // ========================================================================
    private void AceptarClientes()
    {
        while (corriendo)
        {
            try
            {
                TcpClient socket = listener.AcceptTcpClient();
                socket.NoDelay = true;
                var c = new ConexionCliente(socket);
                conexiones.Agregar(c);
                Log($"Conexion entrante desde {c.Endpoint}");
                new Thread(() => AtenderCliente(c)) { IsBackground = true }.Start();
            }
            catch (ObjectDisposedException) { break; }
            catch (InvalidOperationException) { break; }
            catch (SocketException)
            {
                if (!corriendo) break;
                Thread.Sleep(200);
            }
        }
    }

    private void AtenderCliente(ConexionCliente c)
    {
        try
        {
            c.Enviar(Protocolo.Armar("BIENVENIDO", jugadoresRequeridos));

            string? linea;
            while (corriendo && c.Viva && (linea = c.Lector.ReadLine()) != null)
            {
                try
                {
                    ProcesarMensaje(c, linea.Trim());
                }
                catch (Exception ex)
                {
                    // Un error procesando un mensaje NO debe tumbar al servidor ni al cliente
                    Log($"Error procesando '{linea}' de {c.Endpoint}: {ex.Message}");
                    Error(c, "ERROR_INTERNO", "El servidor no pudo procesar la solicitud");
                }
            }
        }
        catch (IOException) { /* conexion caida abruptamente */ }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { Log($"Error con {c.Endpoint}: {ex.Message}"); }
        finally
        {
            Desconectar(c);
        }
    }

    private void ProcesarMensaje(ConexionCliente c, string linea)
    {
        if (linea.Length == 0 || linea.Length > 256) return;

        string[] p = Protocolo.Partir(linea);
        string cmd = p[0].Trim().ToUpperInvariant();
        Log($"<- {(c.Nombre == "" ? c.Endpoint : c.Nombre)}: {linea}");

        if (cmd == Protocolo.CONECTAR) { Conectar(c, p); return; }
        if (cmd == Protocolo.SALIR) { c.Cerrar(); return; }

        if (c.Nombre == "")
        {
            Error(c, "NO_CONECTADO", "Envie primero CONECTAR|nombre");
            return;
        }

        switch (cmd)
        {
            case Protocolo.TIRAR_DADOS: TirarDados(c); break;
            case Protocolo.COMPRAR_PROPIEDAD: ComprarPropiedad(c); break;
            case Protocolo.NO_COMPRAR: NoComprar(c); break;
            case Protocolo.TERMINAR_TURNO: TerminarTurno(c); break;
            case Protocolo.CONSULTAR_ESTADO: ConsultarEstado(c); break;
            case Protocolo.CONSULTAR_TRANSACCIONES: ConsultarTransacciones(c, p); break;
            case Protocolo.REGISTRAR_TARJETA: RegistrarTarjeta(c); break;
            default: Error(c, "COMANDO_DESCONOCIDO", cmd); break;
        }
    }

    // ========================================================================
    // LOBBY: CONECTAR E INICIO DE PARTIDA
    // ========================================================================
    private void Conectar(ConexionCliente c, string[] p)
    {
        string nombre = p.Length > 1 ? p[1].Trim() : "";
        if (!NombreValido.IsMatch(nombre))
        {
            Error(c, "NOMBRE_INVALIDO", "Use de 1 a 20 caracteres (letras, numeros, espacio, _ - .)");
            return;
        }

        lock (estado)
        {
            if (c.Nombre != "") { Error(c, "YA_CONECTADO", "Ya esta registrado como " + c.Nombre); return; }
            if (partidaIniciada) { Error(c, "PARTIDA_EN_CURSO", "La partida ya inicio"); return; }
            if (conexiones.BuscarPorNombre(nombre) != null) { Error(c, "NOMBRE_EN_USO", "Ese nombre ya esta en uso"); return; }
            if (conexiones.ContarRegistrados() >= jugadoresRequeridos) { Error(c, "PARTIDA_LLENA", "No hay cupos"); return; }

            c.Nombre = nombre;
            int registrados = conexiones.ContarRegistrados();
            c.Enviar(Protocolo.Armar("CONECTADO", nombre));
            Broadcast(Protocolo.Armar("JUGADOR_UNIDO", nombre, registrados, jugadoresRequeridos));

            if (registrados == jugadoresRequeridos) IniciarPartida();
        }
    }

    // Llamar SIEMPRE dentro de lock(estado)
    private void IniciarPartida()
    {
        Banco b = new Banco();
        banco = b;

        int id = 1;
        conexiones.Recorrer(c =>
        {
            if (c.Nombre == "") return;
            var j = new Jugador(id++, c.Nombre, b.getTableroJuego().getHead());
            j.setSaldo(SaldoInicial);
            b.registrarJugador(j);     // queda en Salida y entra a la cola circular de turnos
            c.Jugador = j;
        });

        partidaIniciada = true;
        numeroTurno = 1;

        Broadcast(Protocolo.Armar("INICIO", id - 1, SaldoInicial));
        Broadcast(ConstruirEstado());
        AnunciarTurno();
    }

    // Llamar dentro de lock(estado)
    private void AnunciarTurno()
    {
        Jugador j = banco!.getJugadorActual();
        yaTiroDados = false;
        puedeComprar = false;
        Broadcast(Protocolo.Armar("TURNO", j.getID(), j.getNombre(), numeroTurno));
    }

    // ========================================================================
    // ACCIONES DE JUEGO
    // ========================================================================

    // Valida que la partida este en curso y que sea el turno de este cliente.
    // Devuelve el jugador, o null (ya envio el error al cliente).
    private Jugador? ValidarTurno(ConexionCliente c)
    {
        if (!partidaIniciada || banco == null) { Error(c, "PARTIDA_NO_INICIADA", "La partida aun no inicia"); return null; }
        if (partidaFinalizada) { Error(c, "PARTIDA_FINALIZADA", "La partida ya termino"); return null; }

        Jugador? j = c.Jugador;
        if (j == null) { Error(c, "SIN_JUGADOR", "No participa en esta partida"); return null; }
        if (!j.isActivo()) { Error(c, "ELIMINADO", "Fue eliminado de la partida"); return null; }

        Jugador actual = banco.getJugadorActual();
        if (actual != j) { Error(c, "FUERA_DE_TURNO", $"Es el turno de {actual.getNombre()}"); return null; }
        return j;
    }

    private void TirarDados(ConexionCliente c)
    {
        lock (estado)
        {
            Jugador? j = ValidarTurno(c);
            if (j == null) return;
            if (yaTiroDados) { Error(c, "YA_TIRO_DADOS", "Ya lanzo los dados en este turno"); return; }

            Banco b = banco!;
            (int d1, int d2) = b.TirarDadosDetallado();
            yaTiroDados = true;

            Broadcast(Protocolo.Armar("DADOS", j.getID(), j.getNombre(), d1, d2));

            // El display de la Pico puede tardar: no bloquear el lock del juego
            if (hw != null)
            {
                IDispositivoHardware h = hw;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { h.MostrarDados(d1, d2); }
                    catch (Exception ex) { Log($"Error en display: {ex.Message}"); }
                });
            }

            EjecutarMovimiento(j, d1 + d2);
        }
    }

    // Llamar dentro de lock(estado)
    private void EjecutarMovimiento(Jugador j, int pasos)
    {
        Banco b = banco!;
        int saldoAntes = j.getSaldo();
        bool estabaPreso = j.getEstaEncarcelado();

        // Peek de la carta que saldria si cae en una casilla de evento
        string? cartaProxima = b.getMazoEventos().getActual()?.GetCarta().getDescripcion();

        // ---- Caso: jugador encarcelado (cumple sancion, no se mueve) ----
        if (estabaPreso)
        {
            b.ProcesarTirada(j, pasos);
            if (j.getEstaEncarcelado())
                Broadcast(Protocolo.Armar("EN_CARCEL", j.getID(), j.getNombre(), j.getTurnosPerdidos()));
            else
                Broadcast(Protocolo.Armar("LIBERADO", j.getID(), j.getNombre()));

            PostAccion(j);
            return;
        }

        // ---- Calcular a donde cae (misma logica que Tablero.moverJugadorPorDados) ----
        NodoCasilla head = b.getTableroJuego().getHead();
        NodoCasilla nodo = j.getNodoActual();
        int desdeId = nodo.getCasilla().getIdCasilla();
        bool pasoSalida = false;
        for (int i = 0; i < pasos; i++)
        {
            nodo = nodo.getSiguiente();
            if (nodo == head) pasoSalida = true;
        }
        Casilla aterrizaje = nodo.getCasilla();
        Jugador? duenioDestino = (aterrizaje as Propiedad)?.getDuenio();

        Broadcast(Protocolo.Armar("MOVER", j.getID(), j.getNombre(), desdeId,
                                  aterrizaje.getIdCasilla(), aterrizaje.getNombre()));

        // ---- Banco aplica el movimiento y el efecto de la casilla ----
        bool ok = b.ProcesarTirada(j, pasos);

        int bonus = 0;
        if (pasoSalida)
        {
            bonus = 200;
            LogTx(TipoTransaccion.PremioPasarInicio, "Banco", j.getNombre(), 200, "Paso por la Salida");
        }

        if (!ok)
        {
            // Banco ya ejecuto ManejarBancarrota: jugador inactivo y removido de los turnos
            RegistrarBancarrota(j, duenioDestino, saldoAntes + bonus,
                                $"no pudo pagar en {aterrizaje.getNombre()}");
        }
        else
        {
            int delta = j.getSaldo() - saldoAntes - bonus;   // efecto neto de la casilla

            switch (aterrizaje)
            {
                case Propiedad prop when duenioDestino != null:
                    if (delta < 0)
                        LogTx(TipoTransaccion.PagoAlquiler, j.getNombre(), duenioDestino.getNombre(),
                              -delta, $"Alquiler de {prop.getNombre()}");
                    break;

                case CasillaBanco:
                    if (delta < 0)
                        LogTx(TipoTransaccion.PagoAlBanco, j.getNombre(), "Banco", -delta,
                              $"Pago en {aterrizaje.getNombre()}");
                    break;

                case CasillaEvento:
                    Broadcast(Protocolo.Armar("EVENTO", j.getID(), j.getNombre(), cartaProxima ?? ""));
                    if (delta > 0)
                        LogTx(TipoTransaccion.GananciaPorEvento, "Banco", j.getNombre(), delta, cartaProxima ?? "Carta de evento");
                    else if (delta < 0)
                        LogTx(TipoTransaccion.PerdidaPorEvento, j.getNombre(), "Banco", -delta, cartaProxima ?? "Carta de evento");
                    break;

                case CasillaPolicia:
                    Broadcast(Protocolo.Armar("ENCARCELADO", j.getID(), j.getNombre()));
                    break;
            }

            // Si una carta o la policia lo movieron, avisar la posicion final
            Casilla finalCasilla = j.getNodoActual().getCasilla();
            if (finalCasilla.getIdCasilla() != aterrizaje.getIdCasilla())
                Broadcast(Protocolo.Armar("REUBICADO", j.getID(), j.getNombre(),
                                          finalCasilla.getIdCasilla(), finalCasilla.getNombre()));

            // Regla de eliminacion: un evento pudo dejarlo con saldo negativo
            if (j.getSaldo() < 0)
            {
                b.ManejarBancarrota(j, null);
                RegistrarBancarrota(j, null, 0, "saldo negativo tras un pago obligatorio");
            }
            else if (finalCasilla is Propiedad libre && !libre.tieneDuenio() && !j.getEstaEncarcelado())
            {
                puedeComprar = true;
                Broadcast(Protocolo.Armar("PUEDE_COMPRAR", j.getID(), j.getNombre(),
                                          libre.getIdCasilla(), libre.getNombre(), libre.getPrecioDeCompra()));
            }
        }

        PostAccion(j);
    }

    // Llamar dentro de lock(estado). Actualiza clientes y revisa fin de partida.
    private void PostAccion(Jugador quienActuo)
    {
        Banco b = banco!;
        BroadcastEstado();

        Jugador? ganador = b.verificarGanador();
        if (ganador != null) { FinalizarPartida(ganador, "UNICO_SOBREVIVIENTE"); return; }

        // Si el jugador en turno quedo eliminado, ListaTurnos ya paso el turno al siguiente
        if (!quienActuo.isActivo()) AvanzarTrasEliminacion();
    }

    // Llamar dentro de lock(estado)
    private void AvanzarTrasEliminacion()
    {
        numeroTurno++;
        if (numeroTurno > maxTurnos) FinalizarPorLimiteDeTurnos();
        else AnunciarTurno();
    }

    // Valida las condiciones para decidir sobre una compra. Devuelve el jugador o null.
    // Llamar dentro de lock(estado)
    private Jugador? ValidarDecisionCompra(ConexionCliente c)
    {
        Jugador? j = ValidarTurno(c);
        if (j == null) return null;
        if (!yaTiroDados) { Error(c, "DEBE_TIRAR_PRIMERO", "Primero debe lanzar los dados"); return null; }
        if (!puedeComprar) { Error(c, "SIN_PROPIEDAD_DISPONIBLE", "No hay una propiedad disponible para comprar"); return null; }
        return j;
    }

    private void ComprarPropiedad(ConexionCliente c)
    {
        // Fase 1: validar (bajo lock)
        lock (estado)
        {
            if (ValidarDecisionCompra(c) == null) return;
        }

        // Fase 2: si se exige RFID, leer la tarjeta FUERA del lock (puede tardar hasta 10 s)
        string? uidLeido = null;
        if (exigirRfid)
        {
            c.Enviar(Protocolo.Armar("INFO", "ACERQUE_SU_TARJETA_RFID"));
            uidLeido = hw!.LeerRfid(TimeoutRfidMs);
        }

        // Fase 3: revalidar y ejecutar (bajo lock)
        lock (estado)
        {
            Jugador? j = ValidarDecisionCompra(c);
            if (j == null) return;

            if (exigirRfid)
            {
                if (uidLeido == null) { Error(c, "TARJETA_NO_LEIDA", "No se detecto ninguna tarjeta"); return; }
                if (c.Uid == null) { Error(c, "SIN_TARJETA_REGISTRADA", "Registre su tarjeta con REGISTRAR_TARJETA"); return; }
                if (c.Uid != uidLeido) { Error(c, "TARJETA_INVALIDA", "La tarjeta no pertenece a este jugador"); return; }
            }

            Banco b = banco!;
            var prop = (Propiedad)j.getNodoActual().getCasilla();   // puedeComprar garantiza que es Propiedad

            if (prop.tieneDuenio()) { puedeComprar = false; Error(c, "YA_TIENE_PROPIETARIO", "La propiedad ya tiene dueno"); return; }
            if (j.getSaldo() < prop.getPrecioDeCompra()) { Error(c, "SALDO_INSUFICIENTE", $"Necesita ${prop.getPrecioDeCompra()} y tiene ${j.getSaldo()}"); return; }
            if (!b.ProcesarCompra(j)) { Error(c, "COMPRA_RECHAZADA", "El banco rechazo la compra"); return; }

            puedeComprar = false;
            LogTx(TipoTransaccion.CompraPropiedad, j.getNombre(), "Banco", prop.getPrecioDeCompra(),
                  $"Compra de {prop.getNombre()}");
            Broadcast(Protocolo.Armar("COMPRA", j.getID(), j.getNombre(), prop.getIdCasilla(),
                                      prop.getNombre(), prop.getPrecioDeCompra()));
            BroadcastEstado();
        }
    }

    private void NoComprar(ConexionCliente c)
    {
        lock (estado)
        {
            Jugador? j = ValidarDecisionCompra(c);
            if (j == null) return;

            banco!.ProcesarNoCompra(j);
            puedeComprar = false;
            Broadcast(Protocolo.Armar("NO_COMPRA", j.getID(), j.getNombre(),
                                      j.getNodoActual().getCasilla().getNombre()));
        }
    }

    private void TerminarTurno(ConexionCliente c)
    {
        lock (estado)
        {
            Jugador? j = ValidarTurno(c);
            if (j == null) return;
            if (!yaTiroDados) { Error(c, "DEBE_TIRAR_PRIMERO", "Debe lanzar los dados antes de terminar el turno"); return; }

            if (puedeComprar)
            {
                // Terminar el turno sin decidir equivale a NO_COMPRAR
                puedeComprar = false;
                Broadcast(Protocolo.Armar("NO_COMPRA", j.getID(), j.getNombre(),
                                          j.getNodoActual().getCasilla().getNombre()));
            }

            Broadcast(Protocolo.Armar("FIN_TURNO", j.getID(), j.getNombre()));
            banco!.ProcesarFinTurno();      // avanza la cola circular al siguiente jugador
            numeroTurno++;

            if (numeroTurno > maxTurnos) FinalizarPorLimiteDeTurnos();
            else AnunciarTurno();
        }
    }

    // ========================================================================
    // CONSULTAS
    // ========================================================================
    private void ConsultarEstado(ConexionCliente c)
    {
        lock (estado)
        {
            if (!partidaIniciada || banco == null)
            {
                var nombres = new StringBuilder();
                conexiones.Recorrer(x =>
                {
                    if (x.Nombre == "") return;
                    if (nombres.Length > 0) nombres.Append(',');
                    nombres.Append(x.Nombre);
                });
                c.Enviar(Protocolo.Armar("LOBBY", conexiones.ContarRegistrados(), jugadoresRequeridos, nombres.ToString()));
                return;
            }
            c.Enviar(ConstruirEstado());
            c.Enviar(ConstruirDuenios());
        }
    }

    // CONSULTAR_TRANSACCIONES [| ANTIGUAS | RECIENTES | JUGADOR|nombre | TIPO|nombreTipo]
    private void ConsultarTransacciones(ConexionCliente c, string[] p)
    {
        string modo = p.Length > 1 ? p[1].Trim().ToUpperInvariant() : "ANTIGUAS";
        string filtro = p.Length > 2 ? p[2].Trim() : "";

        lock (estado)
        {
            Func<Transaccion, bool> coincide;
            switch (modo)
            {
                case "ANTIGUAS":
                case "RECIENTES":
                    coincide = _ => true;
                    break;
                case "JUGADOR":
                    if (filtro == "") { Error(c, "FALTA_FILTRO", "Use CONSULTAR_TRANSACCIONES|JUGADOR|nombre"); return; }
                    coincide = t => t.JugadorOrigen.Equals(filtro, StringComparison.OrdinalIgnoreCase) ||
                                    t.JugadorDestino.Equals(filtro, StringComparison.OrdinalIgnoreCase);
                    break;
                case "TIPO":
                    if (!Enum.TryParse<TipoTransaccion>(filtro, true, out var tipo))
                    { Error(c, "TIPO_INVALIDO", "Tipos: " + string.Join(",", Enum.GetNames<TipoTransaccion>())); return; }
                    coincide = t => t.Tipo == tipo;
                    break;
                default:
                    Error(c, "MODO_INVALIDO", "Modos: ANTIGUAS, RECIENTES, JUGADOR, TIPO");
                    return;
            }

            int enviados = 0;
            Action<Transaccion> visitante = t =>
            {
                if (!coincide(t)) return;
                c.Enviar(Protocolo.Armar("TX", t.Identificador, t.NumeroTurno,
                                         t.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"), t.Tipo,
                                         t.JugadorOrigen, t.JugadorDestino, t.Monto, t.Descripcion));
                enviados++;
            };

            if (modo == "RECIENTES") historial.RecorrerInverso(visitante);
            else historial.Recorrer(visitante);

            c.Enviar(Protocolo.Armar("TX_FIN", modo, enviados));
        }
    }

    // ========================================================================
    // RFID: asociar la tarjeta fisica de un jugador
    // ========================================================================
    private void RegistrarTarjeta(ConexionCliente c)
    {
        if (hw == null) { Error(c, "SIN_HARDWARE", "El servidor no tiene lector RFID"); return; }

        c.Enviar(Protocolo.Armar("INFO", "ACERQUE_SU_TARJETA_RFID"));
        string? uid = hw.LeerRfid(TimeoutRfidMs);      // fuera del lock: puede tardar
        if (uid == null) { Error(c, "TARJETA_NO_LEIDA", "No se detecto ninguna tarjeta"); return; }

        lock (estado)
        {
            ConexionCliente? otro = conexiones.BuscarPorUid(uid);
            if (otro != null && otro != c) { Error(c, "TARJETA_EN_USO", "Esa tarjeta ya pertenece a " + otro.Nombre); return; }

            c.Uid = uid;
            c.Enviar(Protocolo.Armar("TARJETA_REGISTRADA", c.Nombre, uid));
        }
    }

    // ========================================================================
    // BANCARROTA, DESCONEXION Y FIN DE PARTIDA
    // ========================================================================

    // Registra la transaccion de liquidacion y avisa. Banco.ManejarBancarrota ya debio ejecutarse.
    // Llamar dentro de lock(estado)
    private void RegistrarBancarrota(Jugador quebrado, Jugador? acreedor, int montoTransferido, string motivo)
    {
        if (montoTransferido > 0)
        {
            LogTx(acreedor != null ? TipoTransaccion.PagoEntreJugadores : TipoTransaccion.PagoAlBanco,
                  quebrado.getNombre(), acreedor?.getNombre() ?? "Banco", montoTransferido,
                  $"Liquidacion por bancarrota ({motivo})");
        }
        Broadcast(Protocolo.Armar("BANCARROTA", quebrado.getID(), quebrado.getNombre(),
                                  acreedor?.getNombre() ?? "Banco", motivo));
    }

    private void Desconectar(ConexionCliente c)
    {
        c.Cerrar();

        lock (estado)
        {
            Jugador? j = c.Jugador;

            // Conexion sin jugador: estaba en el lobby o nunca envio CONECTAR
            if (j == null)
            {
                bool estabaRegistrado = c.Nombre != "";
                conexiones.Eliminar(c);
                Log($"Cliente {c.Endpoint} desconectado.");
                if (estabaRegistrado && !partidaIniciada)
                    Broadcast(Protocolo.Armar("JUGADOR_SALIO", c.Nombre,
                                              conexiones.ContarRegistrados(), jugadoresRequeridos));
                return;
            }

            // Jugador en partida: se conserva en la lista (para mostrar su estado), pero se elimina del juego
            Log($"Jugador {c.Nombre} desconectado.");
            if (partidaFinalizada || banco == null || !j.isActivo()) return;

            bool eraSuTurno = banco.getJugadorActual() == j;
            int monto = Math.Max(0, j.getSaldo());

            Broadcast(Protocolo.Armar("DESCONECTADO", j.getID(), j.getNombre()));
            banco.ManejarBancarrota(j, null);
            RegistrarBancarrota(j, null, monto, "desconexion del jugador");
            BroadcastEstado();

            Jugador? ganador = banco.verificarGanador();
            if (ganador != null) { FinalizarPartida(ganador, "UNICO_SOBREVIVIENTE"); return; }
            if (eraSuTurno) AvanzarTrasEliminacion();
        }
    }

    private static int Patrimonio(Jugador j)
    {
        return j.getSaldo() + j.getPropiedades().calcularValorTotal();
    }

    // Llamar dentro de lock(estado)
    private void FinalizarPorLimiteDeTurnos()
    {
        Jugador? mejor = null;
        int mejorPatrimonio = int.MinValue;

        conexiones.Recorrer(c =>
        {
            Jugador? j = c.Jugador;
            if (j == null || !j.isActivo()) return;
            int pat = Patrimonio(j);
            Broadcast(Protocolo.Armar("PATRIMONIO", j.getID(), j.getNombre(), pat));
            if (pat > mejorPatrimonio) { mejor = j; mejorPatrimonio = pat; }
        });

        if (mejor != null) FinalizarPartida(mejor, "LIMITE_DE_TURNOS");
    }

    // Llamar dentro de lock(estado)
    private void FinalizarPartida(Jugador ganador, string motivo)
    {
        partidaFinalizada = true;
        Broadcast(Protocolo.Armar("GANADOR", ganador.getID(), ganador.getNombre(), Patrimonio(ganador), motivo));
        ExportarHistorial();
        Broadcast(Protocolo.Armar("FIN_PARTIDA", "El historial fue exportado en el servidor"));
    }

    // Llamar dentro de lock(estado)
    private void ExportarHistorial()
    {
        try { historial.ExportarAArchivoTXT("Historial_Monopoly.txt"); }
        catch (Exception ex) { Log($"No se pudo exportar el historial: {ex.Message}"); }
    }

    // ========================================================================
    // UTILIDADES: BROADCAST, TRANSACCIONES Y ESTADO
    // ========================================================================
    private static void Error(ConexionCliente c, string codigo, string detalle)
    {
        c.Enviar(Protocolo.Armar("ERROR", codigo, detalle));
    }

    private void Broadcast(string mensaje)
    {
        conexiones.Recorrer(c => c.Enviar(mensaje));
        Log("-> TODOS: " + mensaje);
    }

    // Registra la transaccion en el historial (lista doble) y la muestra a todos.
    // Llamar dentro de lock(estado)
    private void LogTx(TipoTransaccion tipo, string origen, string destino, int monto, string descripcion)
    {
        historial.AgregarTransaccion(numeroTurno, tipo, origen, destino, monto, descripcion);
        Broadcast(Protocolo.Armar("NUEVA_TX", numeroTurno, tipo, origen, destino, monto, descripcion));
    }

    private void BroadcastEstado()
    {
        Broadcast(ConstruirEstado());
        Broadcast(ConstruirDuenios());
    }

    // ESTADO|turno|jugadorEnTurno|id,nombre,saldo,posicionId,activo;id,nombre,...
    private string ConstruirEstado()
    {
        var sb = new StringBuilder();
        conexiones.Recorrer(c =>
        {
            Jugador? j = c.Jugador;
            if (j == null) return;
            if (sb.Length > 0) sb.Append(';');
            sb.Append($"{j.getID()},{j.getNombre()},{j.getSaldo()},{j.getNodoActual().getCasilla().getIdCasilla()},{(j.isActivo() ? 1 : 0)}");
        });

        string enTurno = (banco != null && !partidaFinalizada) ? banco.getJugadorActual().getNombre() : "-";
        return Protocolo.Armar("ESTADO", numeroTurno, enTurno, sb.ToString());
    }

    // DUENIOS|propiedadId,nombre,duenioId,hipotecada;...
    private string ConstruirDuenios()
    {
        var sb = new StringBuilder();
        NodoCasilla head = banco!.getTableroJuego().getHead();
        NodoCasilla nodo = head;
        do
        {
            if (nodo.getCasilla() is Propiedad prop && prop.tieneDuenio())
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append($"{prop.getIdCasilla()},{prop.getNombre()},{prop.getDuenio()!.getID()},{(prop.getEstaHipotecada() ? 1 : 0)}");
            }
            nodo = nodo.getSiguiente();
        } while (nodo != head);

        return Protocolo.Armar("DUENIOS", sb.ToString());
    }

    private static void Log(string mensaje)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [SERVIDOR] {mensaje}");
    }
}