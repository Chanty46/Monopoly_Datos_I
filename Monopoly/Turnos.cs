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
    NodoTurno tail; 
    
}
