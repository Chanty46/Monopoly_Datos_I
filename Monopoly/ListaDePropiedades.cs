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

    // Getters y Setters
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

    //Getters 


    // Agregar una propiedad comprada al final de la lista
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

    // Calcular el valor total de las propiedades (para calcular el patrimonio final)
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

    // De todas las propiedades, buscar si se tiene todas las de un grupo
    // la logica seria buscar entre todas las listas y ver si se tiene un grupo completo
}