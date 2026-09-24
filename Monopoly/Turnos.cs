namespace Monopoly;

class NodoDado
{
    private int valor;
    private NodoDado siguiente;

    public NodoDado(int valor)
    {
        this.valor = valor;
        siguiente = null;
    }

    public int getValor() => valor;
    public NodoDado getSiguiente() => siguiente;
    public void setSiguiente(NodoDado siguiente) => this.siguiente = siguiente;
}

class ListaDados
{
    private NodoDado cabeza;
    private NodoDado actual;

    public ListaDados(params int[] valores)
    {
        cabeza = null;
        actual = null;

        foreach (var valor in valores)
        {
            agregar(valor);
        }
    }

    public void agregar(int valor)
    {
        NodoDado nuevo = new NodoDado(valor);
        if (cabeza == null)
        {
            cabeza = nuevo;
            cabeza.setSiguiente(cabeza);
            actual = cabeza;
            return;
        }

        NodoDado ultimo = cabeza;
        while (ultimo.getSiguiente() != cabeza)
        {
            ultimo = ultimo.getSiguiente();
        }

        ultimo.setSiguiente(nuevo);
        nuevo.setSiguiente(cabeza);
    }

    public string SiguientePar()
    {
        if (actual == null)
        {
            return "DADOS:0,0";
        }

        int primero = actual.getValor();
        actual = actual.getSiguiente();

        int segundo = actual.getValor();
        actual = actual.getSiguiente();

        return $"DADOS:{primero},{segundo}";
    }
}

class NodoTurno
{
    private Jugador jugador;
    private int DadosPasos; // El Nodo del Turno guarda cuantos pasos va a dar, cuestion de ordenar la logica en lo correspondiente
    private NodoTurno siguiente; 

    public NodoTurno(Jugador jugador)
    {
        this.jugador = jugador;
        DadosPasos = 0;
        siguiente = null;
    }

    // Getters y Setters
    public Jugador getJugador() => jugador;
    public int getDadosPasos() => DadosPasos;
    public NodoTurno getSiguiente() => siguiente;

    public void setDadosPasos(int pasos) => DadosPasos = pasos;
    public void setSiguiente(NodoTurno siguiente) => this.siguiente = siguiente;
}

class ListaTurnos
{
    private NodoTurno actual; // Apunta al jugador que tiene el turno
    private NodoTurno tail;   // Para mantener la estructura circular

    public ListaTurnos()
    {
        actual = null;
        tail = null;
    }

    public void agregarJugador(Jugador jugador)
    {
        NodoTurno nuevoNodo = new NodoTurno(jugador);
        if (tail == null)
        {
            tail = nuevoNodo;
            tail.setSiguiente(tail); // si la lsita está vacía, entonces el elemento se enlazara a sí mismo
            actual = tail;
        }
        else
        {
            nuevoNodo.setSiguiente(tail.getSiguiente()); //introducimos el nuevo nodo entre el tail y el primer elemento
            tail.setSiguiente(nuevoNodo);
            tail = nuevoNodo; // actualiza la referencia tail
        }
    }

    // retorna que jugador tiene el turno actual
    public Jugador getTurnoActual()
    {
        if (actual == null) return null;
        return actual.getJugador();
    }

    // avanza un turno en la lista circular
    public void avanzarTurno()
    {
        if (actual != null)
        {
            actual = actual.getSiguiente();
        }
    }
}
