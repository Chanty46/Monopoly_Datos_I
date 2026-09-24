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
    public bool tieneGrupo(Propiedad propiedad) //Esto nos sirve para buscar los grupo
    {
        if(head == null)
        {
            return false; //Simplemente no tiene nada jaja
        }
        string grupoRevisar = propiedad.getGrupo();
        int cantidadGrupo = propiedad.getGrupoSize();
        int cantidadActual = 0;

        NodoPropiedad aux = head;

        while(aux != null)
        {
            if(aux.getPropiedad().getGrupo() == grupoRevisar)
            {
                cantidadActual++;
            }
            aux = aux.getSiguiente();
        }
        return cantidadActual == cantidadGrupo;
    }
    
    public bool puedeMejorar(Propiedad propiedadAConstruir)
{
    string grupo = propiedadAConstruir.getGrupo();
    int casasActuales = propiedadAConstruir.getCasasPuestas();

    // Si ya tiene hotel o 5 casas, no se puede mejorar más
    if (propiedadAConstruir.getTieneHotel() || casasActuales == 5) return false;

    NodoPropiedad aux = head;
    while (aux != null)
    {
        Propiedad propiedadRevisada = aux.getPropiedad();

        // Solo nos interesan las OTRAS propiedades del MISMO grupo
        if (propiedadRevisada.getGrupo() == grupo && propiedadRevisada.getIdCasilla() != propiedadAConstruir.getIdCasilla())
        {
            // Regla: No podés subir esta propiedad si otra del mismo grupo tiene MENOS casas
            if (propiedadRevisada.getCasasPuestas() < casasActuales)   // Osea por ejemplo si tengo P1 P2 y P3 
            {                                                          // Si P1 tiene 1 casas, P2 tiene 1 y P3 tiene 0, no puedo subir a P1 a dos casas pues P3 no tendria 
                return false;                                           // un crecimiento progresivo y entonces no cumple la regla
            }
        }
        aux = aux.getSiguiente();
    } 

    return true; // Cumple con la regla para todas las demás del grupo
}

}

