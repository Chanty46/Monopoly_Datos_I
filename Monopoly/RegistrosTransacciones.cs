using System;

using System.IO;
using System.Text;

namespace Monopoly;

#region 1. TIPO DE TRANSACCIÓN
internal enum TipoTransaccion  //requerimentos minimos
{
    CompraPropiedad,
    PagoAlquiler,
    PagoAlBanco,
    PagoEntreJugadores,
    GananciaPorEvento,
    PerdidaPorEvento,
    PremioPasarInicio
}
#endregion

#region 2. NODO TRANSACCIÓN (Lista Doble)
internal class Transaccion
{
    // Atributos requeridos
    public int Identificador { get; private set; }
    public DateTime FechaHora { get; private set; }
    public int NumeroTurno { get; private set; }
    public TipoTransaccion Tipo { get; private set; }
    public string JugadorOrigen { get; private set; }
    public string JugadorDestino { get; private set; }
    public int Monto { get; private set; }
    public string Descripcion { get; private set; }

    // Punteros para la Lista Doblemente Enlazada
    public Transaccion? Siguiente { get; set; }
    public Transaccion? Anterior { get; set; }


    public Transaccion(int id, int turno, TipoTransaccion tipo, string origen, string destino, int monto, string descripcion)
    {
        Identificador = id;
        FechaHora = DateTime.Now;
        NumeroTurno = turno;
        Tipo = tipo;
        JugadorOrigen = string.IsNullOrWhiteSpace(origen) ? "Banco" : origen;  //si el jugador es nulo
        JugadorDestino = string.IsNullOrWhiteSpace(destino) ? "Banco" : destino; //devuelve banco, sino, devuelve jugador
        Monto = monto;
        Descripcion = descripcion;
        Siguiente = null;
        Anterior = null;
    }

    public string ObtenerTextoFormateado() //le da formato a la data
    {
        return $"[{Identificador}] | Turno: {NumeroTurno} | Fecha: {FechaHora:yyyy-MM-dd HH:mm:ss} | Tipo: {Tipo} | Origen: {JugadorOrigen} | Destino: {JugadorDestino} | Monto: ${Monto} | Detalle: {Descripcion}";
    }
}
#endregion

#region 3. ESTRUCTURA LISTA DOBLEMENTE ENLAZADA
internal class ListaDobleTransacciones
{
    private Transaccion? head;
    private Transaccion? tail;
    private int contadorId;

    public ListaDobleTransacciones()
    {
        head = null;
        tail = null;
        contadorId = 1;
    }

    // agrega una transacción al final de la lista
    public void AgregarTransaccion(int turno, TipoTransaccion tipo, string origen, string destino, int monto, string descripcion)
    {
        Transaccion nueva = new Transaccion(contadorId++, turno, tipo, origen, destino, monto, descripcion);

        if (head == null)
        {
            head = nueva;
            tail = nueva;
        }
        else
        {
            tail!.Siguiente = nueva;
            nueva.Anterior = tail;
            tail = nueva;
        }
    }

    //recorre desde la más antigua (Head -> Tail)
public void ImprimirDesdeMasAntigua()    //terminal
    {
        if (head == null)
        {
            Console.WriteLine("No hay transacciones registradas.");
            return;
        }

        Console.WriteLine("--- HISTORIAL (MÁS ANTIGUO A MÁS RECIENTE) ---");
        Transaccion? actual = head;
        while (actual != null)
        {
            Console.WriteLine(actual.ObtenerTextoFormateado());
            actual = actual.Siguiente;
        }
    }

    // Recorrer desde la más reciente (Tail -> Head)
public void ImprimirDesdeMasReciente()
    {
        if (tail == null)
        {
            Console.WriteLine("No hay transacciones registradas.");
            return;
        }

        Console.WriteLine("--- HISTORIAL (MÁS RECIENTE A MÁS ANTIGUO) ---");
        Transaccion? actual = tail;
        while (actual != null)
        {
            Console.WriteLine(actual.ObtenerTextoFormateado());
            actual = actual.Anterior;
        }
    }

 //busca e imprime transacciones por jugador, tanto emisos como remitenete
    public void BuscarEImprimirPorJugador(string nombreJugador)
    {
        Transaccion? actual = head;
        bool encontrado = false;

        Console.WriteLine($"--- TRANSACCIONES DE: {nombreJugador} ---");
        while (actual != null)
        {
            if (actual.JugadorOrigen.Equals(nombreJugador, StringComparison.OrdinalIgnoreCase) ||
                actual.JugadorDestino.Equals(nombreJugador, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(actual.ObtenerTextoFormateado());
                encontrado = true;
            }
            actual = actual.Siguiente;
        }

        if (!encontrado)
        {
            Console.WriteLine($"No se encontraron transacciones para el jugador '{nombreJugador}'.");
        }
    }
    // Buscar transacciones por tipo
public void BuscarEImprimirPorTipo(TipoTransaccion tipo)
    {
        Transaccion? actual = head;
        bool encontrado = false;

        Console.WriteLine($"--- TRANSACCIONES DEL TIPO: {tipo} ---");
        while (actual != null)
        {
            if (actual.Tipo == tipo)
            {
                Console.WriteLine(actual.ObtenerTextoFormateado());
                encontrado = true;
            }
            actual = actual.Siguiente;
        }

        if (!encontrado)
        {
            Console.WriteLine($"No se encontraron transacciones del tipo '{tipo}'.");
        }
    }
    // Imprimir todas las transacciones en consola
    public void ImprimirTodas()
    {
        Transaccion? actual = head;
        if (actual == null)
        {
            Console.WriteLine("No hay transacciones registradas.");
            return;
        }

        while (actual != null)
        {
            Console.WriteLine(actual.ObtenerTextoFormateado());
            actual = actual.Siguiente;
        }
    }

    // Exportar el historial a un archivo TXT
    public void ExportarAArchivoTXT(string rutaArchivo = "Historial_Monopoly.txt")
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=========================================================================================");
        sb.AppendLine("                                HISTORIAL DE TRANSACCIONES                               ");
        sb.AppendLine("=========================================================================================");

        Transaccion? actual = head;
        while (actual != null)
        {
            sb.AppendLine(actual.ObtenerTextoFormateado());
            actual = actual.Siguiente;
        }

        File.WriteAllText(rutaArchivo, sb.ToString()); //escribe el archivo
        Console.WriteLine($"[+] Historial exportado con éxito a: {Path.GetFullPath(rutaArchivo)}"); //mensaje de confirmacion
    }
}
#endregion