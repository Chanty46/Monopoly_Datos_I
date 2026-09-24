namespace Monopoly;

class NodoTurno
{
    private Jugador jugador;
    private int dadosPasos;
    private NodoTurno siguiente;

    public NodoTurno(Jugador jugador)
    {
        this.jugador = jugador;
        this.dadosPasos = 0;
        this.siguiente = null;
    }

    // ================= GETTERS Y SETTERS =================
    public Jugador getJugador() { return jugador; }
    public int getDadosPasos() { return dadosPasos; }
    public NodoTurno getSiguiente() { return siguiente; }

    public void setDadosPasos(int pasos) { dadosPasos = pasos; }
    public void setSiguiente(NodoTurno sig) { siguiente = sig; }
}

class ListaTurnos
{
    private NodoTurno tail;
    private NodoTurno actual;
    private Random random;
    private int cantidadJugadores; // Guardamos cuántos jugadores activos quedan

    public ListaTurnos()
    {
        tail = null;
        actual = null;
        random = new Random();
        cantidadJugadores = 0;
    }

    public void agregarJugador(Jugador nuevoJugador)
    {
        NodoTurno nuevo = new NodoTurno(nuevoJugador);

        if (tail == null)
        {
            tail = nuevo;
            tail.setSiguiente(tail);
            actual = tail;
        }
        else
        {
            nuevo.setSiguiente(tail.getSiguiente());
            tail.setSiguiente(nuevo);
            tail = nuevo;
        }
        cantidadJugadores++; // Incrementamos el contador de jugadores activos
    }

    public void eliminarJugadorBancarrota(Jugador jugadorEliminar)
    {
        if (actual == null || cantidadJugadores <= 1)
        {
            return; // No se puede eliminar si la lista está vacía o si solo queda el ganador
        }

        // Buscamos el nodo anterior al que queremos eliminar
        NodoTurno aux = actual;
        while (aux.getSiguiente().getJugador() != jugadorEliminar)
        {
            aux = aux.getSiguiente();
        }

        NodoTurno aEliminar = aux.getSiguiente();

        // Si el nodo a eliminar es el que tiene el turno actual, avanzamos 'actual'
        if (aEliminar == actual)
        {
            actual = actual.getSiguiente();
        }

        // Si el nodo a eliminar era la cola (tail), el puntero anterior pasa a ser la nueva cola
        if (aEliminar == tail)
        {
            tail = aux;
        }

        // Sorteamos el nodo eliminado
        aux.setSiguiente(aEliminar.getSiguiente());

        cantidadJugadores--; // Restamos al contador
    }

    public bool haySoloUnJugador() //Esta funcion de aqui la tenemos como helper para ayudar a revisar un ganador
    {
        if (cantidadJugadores == 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public int getCantidadJugadores()
    {
        return cantidadJugadores;
    }

    public Jugador getTurnoActual()
    {
        return actual.getJugador();
    }

    public void avanzarTurno()
    {
        actual = actual.getSiguiente();
    }
}