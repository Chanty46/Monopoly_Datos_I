using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace MonopolyDistribuido;

public class ServidorWeb
{
    private readonly Servidor _servidor;
    private readonly int _puerto;
    private HttpListener? _listener;
    private bool _corriendo = true;
    private Thread? _hiloServidor;

    public ServidorWeb(Servidor servidor, int puerto = 8080)
    {
        _servidor = servidor;
        _puerto = puerto;
    }

    public void Iniciar()
    {
        _hiloServidor = new Thread(EscucharPeticiones)
        {
            IsBackground = true,
            Name = "ServidorWebThread"
        };
        _hiloServidor.Start();
    }

    private void EscucharPeticiones()
    {
        try
        {
            _listener = new HttpListener();
            
            // Intentar escuchar en localhost y en todas las interfaces si el SO lo permite
            try
            {
                _listener.Prefixes.Add($"http://*:{_puerto}/");
                _listener.Start();
            }
            catch
            {
                _listener.Close();
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_puerto}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{_puerto}/");
                _listener.Start();
            }

            Console.WriteLine($"[WEB GUI] Tablero gráfico disponible en: http://localhost:{_puerto}");

            while (_corriendo && _listener.IsListening)
            {
                try
                {
                    var contexto = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem((_) => ProcesarPeticion(contexto));
                }
                catch
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WEB GUI WARNING] No se pudo iniciar el servidor web en puerto {_puerto}: {ex.Message}");
        }
    }

    private void ProcesarPeticion(HttpListenerContext contexto)
    {
        try
        {
            var req = contexto.Request;
            var res = contexto.Response;

            // Habilitar CORS para integración abierta
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 200;
                res.Close();
                return;
            }

            string path = req.Url?.AbsolutePath.ToLowerInvariant() ?? "/";

            if (path == "/" || path == "/index.html")
            {
                byte[] htmlBytes = Encoding.UTF8.GetBytes(GenerarPaginaHtml());
                res.ContentType = "text/html; charset=utf-8";
                res.ContentLength64 = htmlBytes.Length;
                res.OutputStream.Write(htmlBytes, 0, htmlBytes.Length);
            }
            else if (path == "/api/estado")
            {
                string json = _servidor.ObtenerEstadoJson();
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                res.ContentType = "application/json; charset=utf-8";
                res.ContentLength64 = jsonBytes.Length;
                res.OutputStream.Write(jsonBytes, 0, jsonBytes.Length);
            }
            else if (path == "/api/accion" && req.HttpMethod == "POST")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                string body = reader.ReadToEnd();
                
                string accion = "";
                int? idCasilla = null;
                int? idJugador = null;
                string? nombre = null;

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("accion", out var actElem)) accion = actElem.GetString() ?? "";
                    if (doc.RootElement.TryGetProperty("idCasilla", out var idElem) && idElem.TryGetInt32(out int idVal)) idCasilla = idVal;
                    if (doc.RootElement.TryGetProperty("idJugador", out var jElem) && jElem.TryGetInt32(out int jVal)) idJugador = jVal;
                    if (doc.RootElement.TryGetProperty("nombre", out var nElem)) nombre = nElem.GetString();
                }
                catch { }

                var (ok, mensaje) = _servidor.EjecutarAccionWeb(accion, idCasilla, idJugador, nombre);
                string respuestaJson = JsonSerializer.Serialize(new { ok, mensaje });

                byte[] resBytes = Encoding.UTF8.GetBytes(respuestaJson);
                res.ContentType = "application/json; charset=utf-8";
                res.ContentLength64 = resBytes.Length;
                res.OutputStream.Write(resBytes, 0, resBytes.Length);
            }
            else
            {
                res.StatusCode = 404;
            }

            res.Close();
        }
        catch { }
    }

    public void Detener()
    {
        _corriendo = false;
        try
        {
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
        catch { }
    }

    private static string GenerarPaginaHtml()
    {
        return @"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Monopoly Distribuido - Panel Gráfico Oficial</title>
    <style>
        :root {
            --bg-color: #0b1120;
            --board-bg: #f1f5f9;
            --text-main: #f8fafc;
            --panel-bg: #1e293b;
            --card-border: #334155;
            --accent: #38bdf8;
            --success: #22c55e;
            --warn: #f59e0b;
            --danger: #ef4444;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI', system-ui, sans-serif; }
        body { background: var(--bg-color); color: var(--text-main); min-height: 100vh; padding: 14px; display: flex; flex-direction: column; }
        header { display: flex; justify-content: space-between; align-items: center; padding: 10px 18px; background: var(--panel-bg); border-radius: 12px; margin-bottom: 12px; border: 1px solid var(--card-border); }
        .logo-title { display: flex; align-items: center; gap: 12px; }
        .logo-title h1 { font-size: 1.5rem; font-weight: 800; letter-spacing: 1px; color: #fff; }
        .badge-live { background: var(--danger); color: white; padding: 3px 8px; border-radius: 9999px; font-size: 0.7rem; font-weight: bold; animation: pulse 2s infinite; }
        .main-container { display: grid; grid-template-columns: 1fr 380px; gap: 14px; flex: 1; }

        /* Tablero Grid 7x7 para 24 casillas */
        .board-wrapper { display: flex; justify-content: center; align-items: center; background: var(--panel-bg); border-radius: 16px; padding: 16px; border: 1px solid var(--card-border); }
        .board { display: grid; grid-template-columns: repeat(7, 1fr); grid-template-rows: repeat(7, 1fr); gap: 4px; width: 680px; height: 680px; background: #64748b; border: 4px solid #475569; border-radius: 12px; padding: 4px; }
        .tile { background: var(--board-bg); color: #0f172a; border-radius: 6px; padding: 3px; display: flex; flex-direction: column; font-size: 0.65rem; font-weight: bold; position: relative; overflow: hidden; border: 1px solid #cbd5e1; }
        .tile-header { height: 14px; width: 100%; border-radius: 3px 3px 0 0; margin-bottom: 2px; }
        .tile-corner { background: #cbd5e1; text-align: center; justify-content: center; align-items: center; font-size: 0.72rem; }
        .tile-name { flex: 1; text-align: center; display: flex; align-items: center; justify-content: center; line-height: 1.1; }
        .tile-price { text-align: center; font-size: 0.65rem; color: #334155; }
        .tokens-container { position: absolute; bottom: 2px; left: 2px; right: 2px; display: flex; gap: 2px; justify-content: center; flex-wrap: wrap; }
        .player-token { width: 15px; height: 15px; border-radius: 50%; border: 2px solid white; box-shadow: 0 1px 3px rgba(0,0,0,0.6); font-size: 0.55rem; color: white; display: flex; align-items: center; justify-content: center; font-weight: bold; }
        .owner-badge { position: absolute; top: 2px; right: 2px; width: 10px; height: 10px; border-radius: 50%; border: 1px solid white; }
        .tile.mortgaged { opacity: 0.5; background: #fee2e2; }

        /* Centro del tablero */
        .board-center { grid-column: 2 / 7; grid-row: 2 / 7; background: #0f172a; border-radius: 10px; display: flex; flex-direction: column; align-items: center; justify-content: space-between; padding: 14px; text-align: center; border: 2px dashed #334155; position: relative; }
        .center-logo { font-size: 1.8rem; font-weight: 900; color: #f43f5e; letter-spacing: 2px; }
        .center-turn { font-size: 1rem; color: var(--accent); font-weight: bold; }

        /* Display 7 Segmentos Virtual */
        .segment-display { background: #000; border: 2px solid #334155; border-radius: 8px; padding: 4px 12px; display: flex; align-items: center; gap: 8px; box-shadow: 0 0 12px rgba(239, 68, 68, 0.4); }
        .segment-label { font-size: 0.62rem; color: #94a3b8; text-transform: uppercase; letter-spacing: 1px; }
        .segment-digits { font-family: 'Courier New', monospace; font-size: 1.8rem; color: #ef4444; font-weight: 900; text-shadow: 0 0 8px #ef4444; }

        .dice-container { display: flex; gap: 10px; }
        .die { width: 40px; height: 40px; background: white; border-radius: 8px; display: flex; align-items: center; justify-content: center; font-size: 1.4rem; font-weight: 900; color: #0f172a; box-shadow: 0 3px 6px rgba(0,0,0,0.4); }

        /* Panel de Propiedad Disponible */
        .property-banner { background: #1e293b; border: 2px solid #38bdf8; border-radius: 8px; padding: 8px 12px; width: 90%; max-width: 380px; box-shadow: 0 4px 12px rgba(56, 189, 248, 0.2); display: none; }
        .property-banner h4 { color: #38bdf8; font-size: 0.85rem; margin-bottom: 4px; }
        .prop-info-row { display: flex; justify-content: space-between; font-size: 0.78rem; margin: 2px 0; color: #cbd5e1; }

        /* Controles Web */
        .web-controls { display: flex; gap: 6px; flex-wrap: wrap; justify-content: center; width: 100%; }
        button { background: #0284c7; color: white; border: none; padding: 7px 12px; border-radius: 6px; font-weight: bold; font-size: 0.8rem; cursor: pointer; transition: 0.15s; }
        button:hover:not(:disabled) { background: #0369a1; transform: translateY(-1px); }
        button:disabled { background: #334155; color: #64748b; cursor: not-allowed; opacity: 0.6; }
        button.btn-success { background: #16a34a; } button.btn-success:hover:not(:disabled) { background: #15803d; }
        button.btn-warn { background: #d97706; } button.btn-warn:hover:not(:disabled) { background: #b45309; }
        button.btn-danger { background: #dc2626; } button.btn-danger:hover:not(:disabled) { background: #b91c1c; }
        button.btn-secondary { background: #475569; } button.btn-secondary:hover:not(:disabled) { background: #334155; }

        /* Barra de navegación de acciones */
        .actions-nav { display: flex; gap: 6px; justify-content: center; flex-wrap: wrap; margin-top: 4px; }

        /* Paneles laterales */
        .sidebar { display: flex; flex-direction: column; gap: 12px; }
        .panel { background: var(--panel-bg); border-radius: 12px; padding: 12px; border: 1px solid var(--card-border); }
        .panel h3 { font-size: 0.9rem; margin-bottom: 8px; color: var(--accent); display: flex; justify-content: space-between; align-items: center; }

        /* Tarjetas de Jugadores (Siempre 3) */
        .player-slot { padding: 8px 10px; border-radius: 8px; background: #0f172a; margin-bottom: 8px; border-left: 5px solid #64748b; transition: 0.2s; }
        .player-slot.active-turn { box-shadow: 0 0 10px rgba(56, 189, 248, 0.3); border-color: var(--accent); }
        .player-slot.waiting { border-left-color: #475569; opacity: 0.6; }
        .player-slot-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px; }
        .player-name { font-weight: bold; font-size: 0.85rem; display: flex; align-items: center; gap: 6px; }
        .player-badge { width: 10px; height: 10px; border-radius: 50%; }
        .player-balance { font-family: monospace; font-size: 0.92rem; color: #4ade80; font-weight: bold; }
        .player-sub { display: flex; justify-content: space-between; align-items: center; font-size: 0.72rem; color: #94a3b8; margin-top: 2px; }
        .rfid-badge { font-size: 0.68rem; padding: 2px 6px; border-radius: 9999px; font-weight: bold; }
        .rfid-identificado { background: #166534; color: #86efac; }
        .rfid-esperando { background: #854d0e; color: #fef08a; animation: pulse 2s infinite; }
        .rfid-omitido { background: #334155; color: #cbd5e1; }
        .rfid-no_disponible { background: #1e293b; color: #64748b; }
        .rfid-btn-group { display: flex; gap: 4px; margin-top: 4px; }
        .btn-mini { padding: 2px 6px; font-size: 0.68rem; border-radius: 4px; }

        /* Historial de transacciones */
        .tx-log { max-height: 200px; overflow-y: auto; font-size: 0.72rem; color: #cbd5e1; display: flex; flex-direction: column; gap: 4px; }
        .tx-item { padding: 4px 6px; background: #0f172a; border-radius: 4px; border-left: 2px solid var(--accent); }

        /* Toast de notificaciones */
        #toast { position: fixed; bottom: 20px; right: 20px; background: #1e293b; color: #fff; padding: 10px 16px; border-radius: 8px; border: 1px solid var(--accent); box-shadow: 0 4px 15px rgba(0,0,0,0.5); z-index: 1000; font-size: 0.85rem; display: none; }

        /* Modales */
        .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.7); display: none; justify-content: center; align-items: center; z-index: 999; }
        .modal-content { background: var(--panel-bg); border-radius: 12px; width: 90%; max-width: 600px; max-height: 80vh; overflow-y: auto; padding: 18px; border: 1px solid var(--card-border); position: relative; }
        .modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
        .modal-header h3 { color: var(--accent); font-size: 1.1rem; }
        .modal-close { background: none; border: none; font-size: 1.2rem; color: #94a3b8; cursor: pointer; }
        .modal-close:hover { color: #fff; }
        table.modal-table { width: 100%; border-collapse: collapse; font-size: 0.75rem; margin-top: 8px; }
        table.modal-table th, table.modal-table td { padding: 6px 8px; text-align: left; border-bottom: 1px solid #334155; }
        table.modal-table th { background: #0f172a; color: var(--accent); }

        @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }
    </style>
</head>
<body>
    <header>
        <div class=""logo-title"">
            <h1>MONOPOLY DISTRIBUIDO</h1>
            <span class=""badge-live"">EN VIVO</span>
        </div>
        <div style=""display: flex; gap: 16px; align-items: center;"">
            <span id=""hw-status"" style=""font-size: 0.8rem; color: #94a3b8;"">Hardware: Verificando...</span>
            <span id=""round-badge"" style=""background: #334155; padding: 3px 8px; border-radius: 6px; font-size: 0.8rem;"">Ronda 1</span>
        </div>
    </header>

    <div class=""main-container"">
        <div class=""board-wrapper"">
            <div class=""board"" id=""monopoly-board"">
                <!-- Se poblará dinámicamente con JavaScript -->
                <div class=""board-center"">
                    <div class=""center-logo"">MONOPOLY</div>
                    <div class=""center-turn"" id=""turn-display"">Esperando jugadores...</div>

                    <div class=""segment-display"">
                        <span class=""segment-label"">7-SEG PICO</span>
                        <span class=""segment-digits"" id=""display-digits"">00</span>
                    </div>

                    <div class=""dice-container"">
                        <div class=""die"" id=""die1"">-</div>
                        <div class=""die"" id=""die2"">-</div>
                    </div>

                    <!-- Panel emergente de compra de propiedad -->
                    <div class=""property-banner"" id=""property-banner"">
                        <h4 id=""prop-banner-title"">🏠 PROPIEDAD DISPONIBLE</h4>
                        <div class=""prop-info-row""><span>Nombre:</span><strong id=""prop-info-name"">-</strong></div>
                        <div class=""prop-info-row""><span>Precio de Compra:</span><strong id=""prop-info-price"" style=""color:#4ade80;"">₡-</strong></div>
                        <div class=""prop-info-row""><span>Alquiler Base:</span><strong id=""prop-info-rent"">₡-</strong></div>
                        <div class=""prop-info-row""><span>Tu Saldo:</span><strong id=""prop-info-saldo"" style=""color:#38bdf8;"">₡-</strong></div>
                        <div style=""display:flex; gap:8px; margin-top:8px;"">
                            <button id=""btn-comprar"" class=""btn-success"" style=""flex:1;"" onclick=""ejecutarCompra()"">🏠 Comprar</button>
                            <button id=""btn-no-comprar"" class=""btn-warn"" style=""flex:1;"" onclick=""enviarAccion('NO_COMPRAR')"">Pasar Compra</button>
                        </div>
                    </div>

                    <!-- Botonera interactiva de acciones (1-9) -->
                    <div class=""web-controls"" id=""controls-container"">
                        <button id=""btn-tirar"" onclick=""enviarAccion('TIRAR_DADOS')"">🎲 Tirar Dados</button>
                        <button id=""btn-terminar"" class=""btn-danger"" onclick=""enviarAccion('TERMINAR_TURNO')"">Terminar Turno</button>
                    </div>

                    <div class=""actions-nav"">
                        <button class=""btn-secondary"" onclick=""abrirModalHipotecas()"">🏠 Hipotecas</button>
                        <button class=""btn-secondary"" onclick=""abrirModalEstado()"">📊 Estado Oficial</button>
                        <button class=""btn-secondary"" onclick=""abrirModalHistorial()"">📜 Historial</button>
                        <button class=""btn-secondary"" id=""btn-conectar-web"" onclick=""abrirModalConectar()"">➕ Conectar Jugador</button>
                    </div>
                </div>
            </div>
        </div>

        <div class=""sidebar"">
            <div class=""panel"">
                <h3>Jugadores Oficiales <span id=""player-count-badge"" style=""color: #fff; font-size: 0.85rem;"">0 / 3</span></h3>
                <div id=""players-list"">
                    <!-- Se generan siempre 3 slots -->
                </div>
            </div>

            <div class=""panel"" style=""flex: 1; display: flex; flex-direction: column;"">
                <h3>Transacciones Recientes</h3>
                <div class=""tx-log"" id=""tx-container"">
                    <div style=""color: #64748b;"">Esperando transacciones...</div>
                </div>
            </div>
        </div>
    </div>

    <!-- Toast de notificaciones -->
    <div id=""toast""></div>

    <!-- Modal Historial de Transacciones -->
    <div class=""modal-overlay"" id=""modal-historial"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <h3>Historial Oficial de Transacciones</h3>
                <button class=""modal-close"" onclick=""cerrarModales()"">&times;</button>
            </div>
            <table class=""modal-table"">
                <thead>
                    <tr><th>Hora</th><th>Ronda</th><th>Tipo</th><th>Origen</th><th>Destino</th><th>Monto</th><th>Detalle</th></tr>
                </thead>
                <tbody id=""historial-tbody""></tbody>
            </table>
            <div style=""margin-top:12px; display:flex; justify-content:flex-end;"">
                <button class=""btn-secondary"" onclick=""cerrarModales()"">Cerrar</button>
            </div>
        </div>
    </div>

    <!-- Modal Estado Oficial -->
    <div class=""modal-overlay"" id=""modal-estado"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <h3>Estado Oficial del Juego</h3>
                <button class=""modal-close"" onclick=""cerrarModales()"">&times;</button>
            </div>
            <div id=""estado-detallado"" style=""font-size:0.85rem; line-height:1.6; color:#cbd5e1;""></div>
            <div style=""margin-top:14px; display:flex; justify-content:flex-end;"">
                <button class=""btn-secondary"" onclick=""cerrarModales()"">Cerrar</button>
            </div>
        </div>
    </div>

    <!-- Modal Hipotecas -->
    <div class=""modal-overlay"" id=""modal-hipotecas"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <h3>Gestión de Hipotecas (Propiedades en Posesión)</h3>
                <button class=""modal-close"" onclick=""cerrarModales()"">&times;</button>
            </div>
            <div id=""hipotecas-list"" style=""font-size:0.82rem;""></div>
            <div style=""margin-top:14px; display:flex; justify-content:flex-end;"">
                <button class=""btn-secondary"" onclick=""cerrarModales()"">Cerrar</button>
            </div>
        </div>
    </div>

    <!-- Modal Conectar Jugador -->
    <div class=""modal-overlay"" id=""modal-conectar"">
        <div class=""modal-content"" style=""max-width:380px;"">
            <div class=""modal-header"">
                <h3>Conectar Nuevo Jugador</h3>
                <button class=""modal-close"" onclick=""cerrarModales()"">&times;</button>
            </div>
            <div style=""display:flex; flex-direction:column; gap:8px;"">
                <label style=""font-size:0.8rem; color:#94a3b8;"">Nombre del Jugador:</label>
                <input type=""text"" id=""nuevo-jugador-nombre"" placeholder=""Ej: Jugador1"" style=""background:#0f172a; border:1px solid #334155; color:#fff; padding:6px 10px; border-radius:6px; font-size:0.85rem;"">
                <button class=""btn-success"" style=""margin-top:6px;"" onclick=""ejecutarConectarWeb()"">Conectar</button>
            </div>
        </div>
    </div>

    <script>
        const PLAYER_COLORS = ['#ef4444', '#3b82f6', '#10b981'];
        let estadoActual = null;
        let operacionEnCurso = false;

        const CASILLAS_COORDS = [
            { col: 7, row: 7 }, // 0: Salida
            { col: 6, row: 7 }, // 1
            { col: 5, row: 7 }, // 2
            { col: 4, row: 7 }, // 3: Evento
            { col: 3, row: 7 }, // 4
            { col: 2, row: 7 }, // 5
            { col: 1, row: 7 }, // 6: Cárcel
            { col: 1, row: 6 }, // 7
            { col: 1, row: 5 }, // 8
            { col: 1, row: 4 }, // 9: Evento
            { col: 1, row: 3 }, // 10
            { col: 1, row: 2 }, // 11
            { col: 1, row: 1 }, // 12: Parqueo
            { col: 2, row: 1 }, // 13
            { col: 3, row: 1 }, // 14
            { col: 4, row: 1 }, // 15: Evento
            { col: 5, row: 1 }, // 16
            { col: 6, row: 1 }, // 17
            { col: 7, row: 1 }, // 18: Policía
            { col: 7, row: 2 }, // 19
            { col: 7, row: 3 }, // 20
            { col: 7, row: 4 }, // 21: Evento
            { col: 7, row: 5 }, // 22
            { col: 7, row: 6 }  // 23
        ];

        const COLOR_GRUPOS = {
            'Café': '#8B4513',
            'Celeste': '#38bdf8',
            'Rosa': '#f43f5e',
            'Naranja': '#fb923c',
            'Rojo': '#ef4444',
            'Amarillo': '#eab308',
            'Verde': '#22c55e',
            'Azul': '#3b82f6'
        };

        function mostrarToast(msg, esError) {
            const toast = document.getElementById('toast');
            toast.innerText = msg;
            toast.style.borderColor = esError ? '#ef4444' : '#38bdf8';
            toast.style.display = 'block';
            setTimeout(function() { toast.style.display = 'none'; }, 3500);
        }

        function crearCasillasTablero(casillas) {
            const board = document.getElementById('monopoly-board');
            const antiguas = board.querySelectorAll('.tile');
            antiguas.forEach(function(c) { c.remove(); });

            casillas.forEach(function(cas, idx) {
                const pos = CASILLAS_COORDS[idx] || { col: 1, row: 1 };
                const tileDiv = document.createElement('div');
                tileDiv.className = 'tile';
                tileDiv.id = 'tile-' + idx;
                tileDiv.style.gridColumn = pos.col;
                tileDiv.style.gridRow = pos.row;

                if (cas.tipo === 'Especial') {
                    tileDiv.classList.add('tile-corner');
                    tileDiv.innerHTML = '<div class=""tile-name"">' + cas.nombre + '</div><div class=""tokens-container"" id=""tokens-' + idx + '""></div>';
                } else if (cas.tipo === 'Evento') {
                    tileDiv.innerHTML = '<div class=""tile-header"" style=""background:#f59e0b;""></div><div class=""tile-name"">SUERTE</div><div class=""tile-price"">#' + idx + '</div><div class=""tokens-container"" id=""tokens-' + idx + '""></div>';
                } else {
                    const color = COLOR_GRUPOS[cas.grupo] || '#64748b';
                    tileDiv.innerHTML = '<div class=""tile-header"" style=""background:' + color + ';""></div><div class=""tile-name"">' + cas.nombre + '</div><div class=""tile-price"">₡' + cas.precio + '</div><div class=""tokens-container"" id=""tokens-' + idx + '""></div>';
                }

                board.appendChild(tileDiv);
            });
        }

        async function actualizarEstado() {
            try {
                const res = await fetch('/api/estado');
                if (!res.ok) return;
                const data = await res.json();
                estadoActual = data;

                // 1. Inicializar tablero
                if (document.querySelectorAll('.tile').length === 0 && data.casillas) {
                    crearCasillasTablero(data.casillas);
                }

                // 2. Encabezado
                document.getElementById('round-badge').innerText = 'Ronda ' + data.ronda;
                document.getElementById('hw-status').innerText = 'Pico: ' + data.hardwareEstado + ' (' + data.hardwarePuerto + ')';
                document.getElementById('display-digits').innerText = (data.displayValor < 10 ? '0' : '') + data.displayValor;
                document.getElementById('turn-display').innerText = data.turnoActualNombre ? 'Turno oficial de: ' + data.turnoActualNombre : 'Esperando jugadores...';

                // 3. Dados
                document.getElementById('die1').innerText = data.dadosUltimos.d1 || '-';
                document.getElementById('die2').innerText = data.dadosUltimos.d2 || '-';

                // 4. Panel de compra de propiedad
                const pBanner = document.getElementById('property-banner');
                if (data.propiedadPendiente) {
                    pBanner.style.display = 'block';
                    document.getElementById('prop-info-name').innerText = data.propiedadPendiente.nombre + ' (Casilla #' + data.propiedadPendiente.id + ')';
                    document.getElementById('prop-info-price').innerText = '₡' + data.propiedadPendiente.precio;
                    document.getElementById('prop-info-rent').innerText = '₡' + data.propiedadPendiente.alquiler;
                    document.getElementById('prop-info-saldo').innerText = '₡' + data.turnoActualSaldo;
                } else {
                    pBanner.style.display = 'none';
                }

                // 5. Botones dinámicos según estado
                const btnTirar = document.getElementById('btn-tirar');
                const btnTerminar = document.getElementById('btn-terminar');
                const hayJugadores = (data.jugadoresConectados || 0) > 0;

                btnTirar.disabled = !hayJugadores || data.dadosLanzados || operacionEnCurso;
                btnTerminar.disabled = !hayJugadores || !data.dadosLanzados || operacionEnCurso;

                // 6. Slots de jugadores (Siempre 3)
                const playersList = document.getElementById('players-list');
                playersList.innerHTML = '';
                const maxJ = data.maxJugadores || 3;
                const conectados = data.jugadoresConectados || 0;
                document.getElementById('player-count-badge').innerText = conectados + ' / ' + maxJ;

                const jugMap = {};
                (data.jugadores || []).forEach(function(j) { jugMap[j.id] = j; });

                document.querySelectorAll('.tokens-container').forEach(function(c) { c.innerHTML = ''; });

                for (let slot = 1; slot <= maxJ; slot++) {
                    const color = PLAYER_COLORS[(slot - 1) % PLAYER_COLORS.length];
                    const j = jugMap[slot];
                    const slotDiv = document.createElement('div');

                    if (!j) {
                        slotDiv.className = 'player-slot waiting';
                        slotDiv.innerHTML = 
                            '<div class=""player-slot-header"">' +
                            '<div class=""player-name""><span class=""player-badge"" style=""background:#475569;""></span>Jugador ' + slot + '</div>' +
                            '<span class=""rfid-badge rfid-no_disponible"">Esperando conexión</span>' +
                            '</div>' +
                            '<div style=""font-size:0.7rem; color:#64748b;"">Slot libre para conexión TCP o Web</div>';
                    } else {
                        const esTurno = (j.id === data.turnoActualId);
                        slotDiv.className = 'player-slot' + (esTurno ? ' active-turn' : '');
                        slotDiv.style.borderLeftColor = color;

                        const rfidEstado = j.rfidEstado || 'no_disponible';
                        let rfidBadge = '';
                        let rfidActions = '';

                        if (rfidEstado === 'identificado') {
                            rfidBadge = '<span class=""rfid-badge rfid-identificado"">✅ RFID: ' + (j.rfidUid || 'OK') + '</span>';
                        } else if (rfidEstado === 'omitido') {
                            rfidBadge = '<span class=""rfid-badge rfid-omitido"">⚪ RFID Omitido (Modo ID)</span>';
                        } else {
                            rfidBadge = '<span class=""rfid-badge rfid-esperando"">⏳ Acerque Tarjeta RFID</span>';
                            rfidActions = 
                                '<div class=""rfid-btn-group"">' +
                                '<button class=""btn-mini btn-secondary"" onclick=""reintentarRfid(' + j.id + ')"">🔄 Reintentar RFID</button>' +
                                '<button class=""btn-mini btn-warn"" onclick=""continuarSinRfid(' + j.id + ')"">✓ Continuar sin RFID</button>' +
                                '</div>';
                        }

                        slotDiv.innerHTML =
                            '<div class=""player-slot-header"">' +
                            '<div class=""player-name""><span class=""player-badge"" style=""background:' + color + ';""></span>' + j.nombre + (esTurno ? ' ⭐ (Turno Actual)' : '') + '</div>' +
                            '<div class=""player-balance"">₡' + j.saldo + '</div>' +
                            '</div>' +
                            '<div class=""player-sub"">' +
                            '<span>📍 ' + j.casillaNombre + ' (#' + j.casillaId + ')</span>' +
                            rfidBadge +
                            '</div>' +
                            rfidActions;

                        // Token en el tablero
                        const tokenContainer = document.getElementById('tokens-' + j.casillaId);
                        if (tokenContainer) {
                            const token = document.createElement('div');
                            token.className = 'player-token';
                            token.style.background = color;
                            token.innerText = j.id;
                            token.title = j.nombre + ' (ID ' + j.id + ')';
                            tokenContainer.appendChild(token);
                        }
                    }

                    playersList.appendChild(slotDiv);
                }

                // 7. Propietarios en casillas
                (data.casillas || []).forEach(function(c) {
                    const tileDiv = document.getElementById('tile-' + c.id);
                    if (!tileDiv) return;

                    const antiguoBadge = tileDiv.querySelector('.owner-badge');
                    if (antiguoBadge) antiguoBadge.remove();

                    if (c.duenioId) {
                        const idxDuenio = (data.jugadores || []).findIndex(function(j) { return j.id === c.duenioId; });
                        const colorDuenio = idxDuenio >= 0 ? PLAYER_COLORS[idxDuenio % PLAYER_COLORS.length] : '#94a3b8';

                        const badge = document.createElement('div');
                        badge.className = 'owner-badge';
                        badge.style.background = colorDuenio;
                        badge.title = 'Dueño: ' + c.duenioNombre;
                        tileDiv.appendChild(badge);

                        if (c.hipotecada) tileDiv.classList.add('mortgaged');
                        else tileDiv.classList.remove('mortgaged');
                    }
                });

                // 8. Transacciones recientes
                const txContainer = document.getElementById('tx-container');
                txContainer.innerHTML = '';
                (data.transacciones || []).forEach(function(tx) {
                    const item = document.createElement('div');
                    item.className = 'tx-item';
                    item.innerText = tx;
                    txContainer.appendChild(item);
                });

            } catch (err) { }
        }

        async function enviarAccion(accion, idCasilla, idJugador, nombre) {
            if (operacionEnCurso) return;
            operacionEnCurso = true;
            try {
                const res = await fetch('/api/accion', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ accion: accion, idCasilla: idCasilla, idJugador: idJugador, nombre: nombre })
                });
                const data = await res.json();
                mostrarToast(data.mensaje, !data.ok);
                actualizarEstado();
            } catch (err) {
                mostrarToast('Error de comunicación con el servidor.', true);
            } finally {
                operacionEnCurso = false;
            }
        }

        async function ejecutarCompra() {
            const btnComprar = document.getElementById('btn-comprar');
            btnComprar.disabled = true;
            btnComprar.innerText = '⏳ Procesando...';
            await enviarAccion('COMPRAR_PROPIEDAD');
            btnComprar.disabled = false;
            btnComprar.innerText = '🏠 Comprar';
        }

        function reintentarRfid(idJugador) {
            mostrarToast('Solicitando lectura RFID al hardware...', false);
            enviarAccion('REINTENTAR_RFID', null, idJugador);
        }

        function continuarSinRfid(idJugador) {
            enviarAccion('CONTINUAR_SIN_RFID', null, idJugador);
        }

        function cerrarModales() {
            document.querySelectorAll('.modal-overlay').forEach(function(m) { m.style.display = 'none'; });
        }

        function abrirModalHistorial() {
            if (!estadoActual) return;
            const tbody = document.getElementById('historial-tbody');
            tbody.innerHTML = '';
            (estadoActual.historial || []).forEach(function(h) {
                const tr = document.createElement('tr');
                tr.innerHTML = '<td>' + h.fecha + '</td><td>' + h.ronda + '</td><td>' + h.tipo + '</td><td>' + h.origen + '</td><td>' + h.destino + '</td><td style=""color:#4ade80; font-weight:bold;"">₡' + h.monto + '</td><td>' + h.detalle + '</td>';
                tbody.appendChild(tr);
            });
            document.getElementById('modal-historial').style.display = 'flex';
        }

        function abrirModalEstado() {
            if (!estadoActual) return;
            const div = document.getElementById('estado-detallado');
            let jHtml = '<h4>Jugadores en Partida:</h4><ul>';
            (estadoActual.jugadores || []).forEach(function(j) {
                jHtml += '<li><strong>' + j.nombre + '</strong> (ID ' + j.id + '): Saldo ₡' + j.saldo + ' | Posición: ' + j.casillaNombre + ' (#' + j.casillaId + ') | RFID: ' + (j.rfidEstado || 'N/A') + '</li>';
            });
            jHtml += '</ul>';

            div.innerHTML = 
                '<p><strong>Ronda Oficial:</strong> ' + estadoActual.ronda + '</p>' +
                '<p><strong>Turno Oficial Actual:</strong> ' + estadoActual.turnoActualNombre + ' (ID ' + estadoActual.turnoActualId + ')</p>' +
                '<p><strong>Dados Lanzados este turno:</strong> ' + (estadoActual.dadosLanzados ? 'Sí' : 'No') + '</p>' +
                '<p><strong>Último valor 7-Segmentos:</strong> ' + estadoActual.displayValor + '</p>' +
                '<p><strong>Hardware Raspberry Pico:</strong> ' + estadoActual.hardwareEstado + ' (' + estadoActual.hardwarePuerto + ')</p>' +
                '<hr style=""border-color:#334155; margin:10px 0;"">' + jHtml;
            document.getElementById('modal-estado').style.display = 'flex';
        }

        function abrirModalHipotecas() {
            if (!estadoActual) return;
            const div = document.getElementById('hipotecas-list');
            div.innerHTML = '';
            const jugadorTurno = (estadoActual.jugadores || []).find(function(j) { return j.id === estadoActual.turnoActualId; });
            if (!jugadorTurno || !jugadorTurno.propiedades || jugadorTurno.propiedades.length === 0) {
                div.innerHTML = '<p style=""color:#94a3b8;"">El jugador en turno no posee propiedades adquiridas.</p>';
            } else {
                let html = '<table class=""modal-table""><thead><tr><th>ID</th><th>Nombre</th><th>Grupo</th><th>Precio</th><th>Estado</th><th>Acción</th></tr></thead><tbody>';
                jugadorTurno.propiedades.forEach(function(p) {
                    const accionBtn = p.hipotecada 
                        ? '<button class=""btn-mini btn-warn"" onclick=""enviarAccion(\'DESHIPOTECAR\', ' + p.id + ')"">Deshipotecar</button>'
                        : '<button class=""btn-mini btn-danger"" onclick=""enviarAccion(\'HIPOTECAR\', ' + p.id + ')"">Hipotecar</button>';
                    html += '<tr><td>' + p.id + '</td><td>' + p.nombre + '</td><td>' + p.grupo + '</td><td>₡' + p.precio + '</td><td>' + (p.hipotecada ? 'Hipotecada' : 'Activa') + '</td><td>' + accionBtn + '</td></tr>';
                });
                html += '</tbody></table>';
                div.innerHTML = html;
            }
            document.getElementById('modal-hipotecas').style.display = 'flex';
        }

        function abrirModalConectar() {
            document.getElementById('nuevo-jugador-nombre').value = '';
            document.getElementById('modal-conectar').style.display = 'flex';
        }

        function ejecutarConectarWeb() {
            const nom = document.getElementById('nuevo-jugador-nombre').value.trim();
            cerrarModales();
            enviarAccion('CONECTAR', null, null, nom);
        }

        // Refresco automático cada 500 ms
        setInterval(actualizarEstado, 500);
        actualizarEstado();
    </script>
</body>
</html>";
    }
}
