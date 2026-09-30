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

public class NodoTransaccion
{
    public Transaccion Transaccion { get; }
    public NodoTransaccion? Siguiente { get; set; }

    public NodoTransaccion(Transaccion transaccion)
    {
        Transaccion = transaccion;
        Siguiente = null;
    }
}

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
            tail = nuevo;
        }
        size++;

        // Guardar automáticamente en archivo según instructivo
        try
        {
            File.AppendAllText(archivoLog, tx.ToCsv() + "\n", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HISTORIAL ERROR] No se pudo escribir en {archivoLog}: {ex.Message}");
        }
    }

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
}
