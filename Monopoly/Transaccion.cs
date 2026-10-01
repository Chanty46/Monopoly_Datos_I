using System;
using System.IO;
using System.Text;

namespace Monopoly;

public class Transaccion
{
    private static int _contadorId = 1;

    public int Id { get; }
    public DateTime Fecha { get; }
    public int Turno { get; }
    public string Tipo { get; }
    public string JugadorOrigen { get; }
    public string JugadorDestino { get; }
    public int Monto { get; }
    public string Descripcion { get; }

    public Transaccion(int turno, string tipo, string origen, string destino, int monto, string descripcion)
    {
        Id = _contadorId++;
        Fecha = DateTime.Now;
        Turno = turno;
        Tipo = tipo;
        JugadorOrigen = origen;
        JugadorDestino = destino;
        Monto = monto;
        Descripcion = descripcion;
    }

    public override string ToString()
    {
        return $"[TX #{Id:D3} | Turno {Turno}] {Fecha:HH:mm:ss} - {Tipo}: {JugadorOrigen} -> {JugadorDestino} | ₡{Monto} | {Descripcion}";
    }

    public string ToCsv()
    {
        return $"{Id},{Fecha:yyyy-MM-dd HH:mm:ss},{Turno},{Tipo},{JugadorOrigen},{JugadorDestino},{Monto},\"{Descripcion}\"";
    }
}

/// <summary>
/// Nodo para la lista doblemente enlazada del historial de transacciones.
/// </summary>
public class NodoTransaccion
{
    public Transaccion Transaccion { get; }
    public NodoTransaccion? Siguiente { get; set; }
    public NodoTransaccion? Anterior { get; set; }

    public NodoTransaccion(Transaccion transaccion)
    {
        Transaccion = transaccion;
        Siguiente = null;
        Anterior = null;
    }
}

/// <summary>
/// Estructura propia: Lista Doblemente Enlazada para el almacenamiento y consulta
/// bidireccional del historial de transacciones (Sección 11, 12 y 13 del instructivo).
/// </summary>
public class HistorialTransacciones
{
    private NodoTransaccion? head;
    private NodoTransaccion? tail;
    private int size;
    private readonly string archivoLog;

    public HistorialTransacciones(string rutaArchivo = "transacciones.txt")
    {
        head = null;
        tail = null;
        size = 0;
        archivoLog = rutaArchivo;

        try
        {
            if (!File.Exists(archivoLog))
            {
                File.WriteAllText(archivoLog, "ID,FECHA,TURNO,TIPO,ORIGEN,DESTINO,MONTO,DESCRIPCION\n", Encoding.UTF8);
            }
        }
        catch { }
    }

    public int GetSize() => size;
    public NodoTransaccion? GetHead() => head;
    public NodoTransaccion? GetTail() => tail;

    /// <summary>
    /// Agrega una transacción al final de la lista doblemente enlazada y persiste en disco.
    /// </summary>
    public void Registrar(Transaccion tx)
    {
        var nuevo = new NodoTransaccion(tx);
        if (head == null)
        {
            head = nuevo;
            tail = nuevo;
        }
        else
        {
            tail!.Siguiente = nuevo;
            nuevo.Anterior = tail;
            tail = nuevo;
        }
        size++;

        // Guardar automáticamente en archivo TXT según Sección 13
        try
        {
            File.AppendAllText(archivoLog, tx.ToCsv() + "\n", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HISTORIAL ERROR] No se pudo escribir en {archivoLog}: {ex.Message}");
        }
    }

    /// <summary>
    /// Recorre la estructura desde la transacción más antigua hacia la más reciente (head -> tail).
    /// </summary>
    public Transaccion[] RecorrerDesdeMasAntigua()
    {
        var resultado = new Transaccion[size];
        var actual = head;
        int i = 0;
        while (actual != null && i < size)
        {
            resultado[i++] = actual.Transaccion;
            actual = actual.Siguiente;
        }
        return resultado;
    }

    /// <summary>
    /// Recorre la estructura desde la transacción más reciente hacia la más antigua (tail -> head).
    /// </summary>
    public Transaccion[] RecorrerDesdeMasReciente()
    {
        var resultado = new Transaccion[size];
        var actual = tail;
        int i = 0;
        while (actual != null && i < size)
        {
            resultado[i++] = actual.Transaccion;
            actual = actual.Anterior;
        }
        return resultado;
    }

    /// <summary>
    /// Busca todas las transacciones donde intervino un jugador específico (como origen o destino).
    /// </summary>
    public Transaccion[] BuscarPorJugador(string nombreJugador)
    {
        if (string.IsNullOrWhiteSpace(nombreJugador) || head == null) return Array.Empty<Transaccion>();

        // Primer pase: contar coincidencias
        int coincidencias = 0;
        var actual = head;
        while (actual != null)
        {
            if (string.Equals(actual.Transaccion.JugadorOrigen, nombreJugador, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actual.Transaccion.JugadorDestino, nombreJugador, StringComparison.OrdinalIgnoreCase))
            {
                coincidencias++;
            }
            actual = actual.Siguiente;
        }

        var resultado = new Transaccion[coincidencias];
        actual = head;
        int idx = 0;
        while (actual != null && idx < coincidencias)
        {
            if (string.Equals(actual.Transaccion.JugadorOrigen, nombreJugador, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actual.Transaccion.JugadorDestino, nombreJugador, StringComparison.OrdinalIgnoreCase))
            {
                resultado[idx++] = actual.Transaccion;
            }
            actual = actual.Siguiente;
        }
        return resultado;
    }

    /// <summary>
    /// Busca todas las transacciones de un tipo específico (ej: COMPRA, ALQUILER, EVENTO, etc).
    /// </summary>
    public Transaccion[] BuscarPorTipo(string tipoTransaccion)
    {
        if (string.IsNullOrWhiteSpace(tipoTransaccion) || head == null) return Array.Empty<Transaccion>();

        int coincidencias = 0;
        var actual = head;
        while (actual != null)
        {
            if (actual.Transaccion.Tipo.Contains(tipoTransaccion, StringComparison.OrdinalIgnoreCase))
            {
                coincidencias++;
            }
            actual = actual.Siguiente;
        }

        var resultado = new Transaccion[coincidencias];
        actual = head;
        int idx = 0;
        while (actual != null && idx < coincidencias)
        {
            if (actual.Transaccion.Tipo.Contains(tipoTransaccion, StringComparison.OrdinalIgnoreCase))
            {
                resultado[idx++] = actual.Transaccion;
            }
            actual = actual.Siguiente;
        }
        return resultado;
    }

    /// <summary>
    /// Imprime todas las transacciones almacenadas formateadas en texto.
    /// </summary>
    public string ImprimirTodas()
    {
        if (head == null) return "No hay transacciones registradas.";

        var sb = new StringBuilder();
        var actual = head;
        while (actual != null)
        {
            sb.AppendLine(actual.Transaccion.ToString());
            actual = actual.Siguiente;
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Retorna las últimas N transacciones en formato texto.
    /// </summary>
    public string ObtenerHistorialTexto(int ultimas = 20)
    {
        if (head == null) return "No hay transacciones registradas.";

        var sb = new StringBuilder();
        var actual = head;
        int saltar = Math.Max(0, size - ultimas);
        int contador = 0;

        while (actual != null)
        {
            if (contador >= saltar)
            {
                sb.AppendLine(actual.Transaccion.ToString());
            }
            contador++;
            actual = actual.Siguiente;
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Exporta manualmente el historial a un archivo de texto especificado (Sección 13).
    /// </summary>
    public bool ExportarATxt(string rutaDestino)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,FECHA,TURNO,TIPO,ORIGEN,DESTINO,MONTO,DESCRIPCION");
            var txs = RecorrerDesdeMasAntigua();
            foreach (var tx in txs)
            {
                sb.AppendLine(tx.ToCsv());
            }
            File.WriteAllText(rutaDestino, sb.ToString(), Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EXPORT ERROR] Error exportando transacciones a {rutaDestino}: {ex.Message}");
            return false;
        }
    }
}
