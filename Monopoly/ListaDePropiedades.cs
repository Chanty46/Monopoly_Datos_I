namespace Monopoly;

class NodoPropiedad
{
    private Propiedad propiedad;
    private NodoPropiedad siguiente;

    public NodoPropiedad(Propiedad propiedad)
    {
        this.propiedad = propiedad;
        siguiente = null;
    }

    public Propiedad getPropiedad() { return propiedad; }
    public NodoPropiedad getSiguiente() { return siguiente; }
    public void setSiguiente(NodoPropiedad siguiente) { this.siguiente = siguiente; }
}

class ListaPropiedades
{
    private NodoPropiedad head;
    private int size;

    public ListaPropiedades()
    {
        head = null;
        size = 0;
    }

    public int getSize() { return size; }
    public NodoPropiedad getHead() { return head; }

    public void agregarPropiedad(Propiedad nuevaPropiedad)
    {
        NodoPropiedad nuevoNodo = new NodoPropiedad(nuevaPropiedad);

        if (head == null)
        {
            head = nuevoNodo;
        }
        else
        {
            NodoPropiedad actual = head;
            while (actual.getSiguiente() != null)
            {
                actual = actual.getSiguiente();
            }
            actual.setSiguiente(nuevoNodo);
        }
        size++;
    }

    public int calcularValorTotal()
    {
        int total = 0;
        NodoPropiedad actual = head;

        while (actual != null)
        {
            total += actual.getPropiedad().getPrecioDeCompra();
            actual = actual.getSiguiente();
        }

        return total;
    }
}