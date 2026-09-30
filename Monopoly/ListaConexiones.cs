namespace MonopolyDistribuido;

// Lista enlazada simple propia (el enunciado prohibe List/LinkedList/etc.)
// para guardar las conexiones. Es thread-safe con un lock interno.
internal class NodoConexion
{
    public ConexionCliente Valor;
    public NodoConexion? Siguiente;

    public NodoConexion(ConexionCliente valor)
    {
        Valor = valor;
        Siguiente = null;
    }
}

internal class ListaConexiones
{
    private NodoConexion? head;
    private NodoConexion? tail;
    private int size;
    private readonly object candado = new();

    public int Contar()
    {
        lock (candado) { return size; }
    }

    // Cuenta las conexiones que ya enviaron CONECTAR (tienen nombre)
    public int ContarRegistrados()
    {
        int n = 0;
        Recorrer(c => { if (c.Nombre != "") n++; });
        return n;
    }

    public void Agregar(ConexionCliente c)
    {
        lock (candado)
        {
            NodoConexion nuevo = new NodoConexion(c);
            if (head == null) { head = nuevo; tail = nuevo; }
            else { tail!.Siguiente = nuevo; tail = nuevo; }
            size++;
        }
    }

    public bool Eliminar(ConexionCliente c)
    {
        lock (candado)
        {
            NodoConexion? previo = null;
            NodoConexion? actual = head;
            while (actual != null)
            {
                if (actual.Valor == c)
                {
                    if (previo == null) head = actual.Siguiente;
                    else previo.Siguiente = actual.Siguiente;
                    if (actual == tail) tail = previo;
                    size--;
                    return true;
                }
                previo = actual;
                actual = actual.Siguiente;
            }
            return false;
        }
    }

    // Recorre en orden de llegada. La accion NO debe agregar/eliminar de la lista.
    public void Recorrer(Action<ConexionCliente> accion)
    {
        lock (candado)
        {
            NodoConexion? actual = head;
            while (actual != null)
            {
                accion(actual.Valor);
                actual = actual.Siguiente;
            }
        }
    }

    public ConexionCliente? BuscarPorNombre(string nombre)
    {
        lock (candado)
        {
            NodoConexion? actual = head;
            while (actual != null)
            {
                if (actual.Valor.Nombre != "" &&
                    actual.Valor.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase))
                    return actual.Valor;
                actual = actual.Siguiente;
            }
            return null;
        }
    }

    public ConexionCliente? BuscarPorUid(string uid)
    {
        lock (candado)
        {
            NodoConexion? actual = head;
            while (actual != null)
            {
                if (actual.Valor.Uid != null && actual.Valor.Uid == uid)
                    return actual.Valor;
                actual = actual.Siguiente;
            }
            return null;
        }
    }
}