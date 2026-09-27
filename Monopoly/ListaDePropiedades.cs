using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

namespace Monopoly;

class NodoPropiedad
{
    private Propiedad propiedad;
    private NodoPropiedad? siguiente;

    public NodoPropiedad(Propiedad propiedad)
    {
        this.propiedad = propiedad;
        siguiente = null;
    }

    // Getters y Setters
    public Propiedad getPropiedad() { return propiedad; }
    public NodoPropiedad? getSiguiente() { return siguiente; }
    public void setSiguiente(NodoPropiedad? siguiente) { this.siguiente = siguiente; }
}

class ListaPropiedades
{
    private NodoPropiedad? head;
    private int size;

    public ListaPropiedades()
    {
        head = null;
        size = 0;
    }

    //Getters 
    public NodoPropiedad? getHead() { return head; }
    public int getSize() { return size; }
    public void setHead(NodoPropiedad? newHead) { head = newHead; }
    public void setSize(int newSize) { size = newSize; }
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

public bool eliminarPropiedad(Propiedad propiedadEliminar) //El bool es para saber si se ejecuto el eliminar la propiedad de manera correcta
{
    if (head == null || propiedadEliminar == null) //Casos extremos si no hay head o la propiedad dada es nula
    {
        return false;
    }

    // Caso 1: La propiedad a eliminar está en la cabeza (head)
    if (head.getPropiedad() == propiedadEliminar)
    {
        head = head.getSiguiente(); // Desplazamos la cabeza al siguiente nodo
        size--;
        return true;
    }

    // Caso 2: La propiedad está en medio o al final de la lista
    NodoPropiedad aux = head;
    while (aux.getSiguiente() != null)
    {
        if (aux.getSiguiente().getPropiedad() == propiedadEliminar)
        {
            // Reconectamos el puntero para saltarnos el nodo a eliminar
            aux.setSiguiente(aux.getSiguiente().getSiguiente());
            size--;
            return true;
        }
        aux = aux.getSiguiente();
    }

    return false; // La propiedad no pertenecía a este jugador
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
        // 1. Ferrocarriles y Servicios nunca pueden tener casas
        if (propiedadAConstruir is Ferrocarril || propiedadAConstruir is CasillaServicio)
        {
            return false;
        }

        // 2. Regla fundamental de Monopoly: Debe tener TODAS las propiedades del grupo de color
        if (!tieneGrupo(propiedadAConstruir))
        {
            return false;
        }

        string grupo = propiedadAConstruir.getGrupo();
        int casasActuales = propiedadAConstruir.getCasasPuestas();

        // Si ya tiene hotel o 5 casas, no se puede mejorar más
        if (propiedadAConstruir.getTieneHotel() || casasActuales == 5) { return false; }

        NodoPropiedad aux = head;
        while (aux != null)
        {
            Propiedad propiedadRevisada = aux.getPropiedad();

            // Solo nos interesan las OTRAS propiedades del MISMO grupo
            if (propiedadRevisada.getGrupo() == grupo && propiedadRevisada.getIdCasilla() != propiedadAConstruir.getIdCasilla())
            {
                // Regla adicional: ninguna propiedad del mismo grupo puede estar hipotecada
                if (propiedadRevisada.getEstaHipotecada())
                {
                    return false;
                }

                // Regla uniforme: No podés subir esta propiedad si otra del mismo grupo tiene MENOS casas
                if (propiedadRevisada.getCasasPuestas() < casasActuales)
                {
                    return false;
                }
            }
            aux = aux.getSiguiente();
        } 

        return true; // Cumple con la regla para todas las demás del grupo
    }
public bool puedeQuitarCasa(Propiedad propiedadAQuitar)
{
    string grupo = propiedadAQuitar.getGrupo();
    int casasDespuesDeQuitar = propiedadAQuitar.getCasasPuestas() - 1; //el nombre de la variable se explica solo

    if (casasDespuesDeQuitar < 0) return false;

    NodoPropiedad aux = head;
    while (aux != null)
    {
        Propiedad propiedadRevisada = aux.getPropiedad();

        if (propiedadRevisada.getGrupo() == grupo && propiedadRevisada.getIdCasilla() != propiedadAQuitar.getIdCasilla())
        {
            if (propiedadRevisada.getCasasPuestas() > casasDespuesDeQuitar) //Esto de aqui es revisar si vamos a quedar desigual, en este caso se compara con las otras, usa la misma logica anterior solo que alreves
            {                                                               //Asi evitamos problemas luego
                return false;
            }
        }
        aux = aux.getSiguiente();
    }

    return true;
}

public int contarFerrocarriles()
    {
        NodoPropiedad aux = head;
        int res = 0;
        while(aux != null)
        {
            if(aux.getPropiedad() is Ferrocarril)
            {
                if(aux.getPropiedad().getEstaHipotecada() == false) 
                {
                    res++; //Seria como contar ferrocarriles validos
                }
            }
            aux = aux.getSiguiente();
        }
        return res;
    }

public int contarServicios()
    {
        NodoPropiedad aux = head;
        int res = 0;
        while(aux != null)
        {
            if(aux.getPropiedad() is CasillaServicio)
            {
                res++;
            }
            aux = aux.getSiguiente();
        }
        return res;

    }

}

