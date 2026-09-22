namespace Monopoly;

class CartaEvento
{
    private int id; //cada carta tiene un id
    private string descripcion; //descripcion de qué hace cada carta, como ganar dinero, perder, avanzar, etc
    private Action<Jugador> accion; //metodo que recibe qué debe hacer al declarar cada carta, sirve para no hacer multiples subclases

    public CartaEvento(int id, string descripcion, Action<Jugador> accion)
    {
        this.id = id;
        this.descripcion = descripcion;
        this.accion = accion;
    }

    // ================= GETTERS Y SETTERS =================
    public int getId() { return id; }
    public string getDescripcion() { return descripcion; }
    public Action<Jugador> getAccion() { return accion; }

    public void setId(int newId) { id = newId; }
    public void setDescripcion(string newDescripcion) { descripcion = newDescripcion; }
    public void setAccion(Action<Jugador> newAccion) { accion = newAccion; }

    // Metodos
    public void aplicarEfecto(Jugador jugador)
    {
        accion?.Invoke(jugador); // ? pregunta si el metodo accion es diferente de null, si sí lo es, entonces invoke llama a su metodo
    }          // Invoke es necesario porque accion no es un método directamente, sino una referencia
}

class NodoCarta
{
    private CartaEvento carta;
    private NodoCarta? siguiente;

    public NodoCarta(CartaEvento carta)
    {
        this.carta = carta;
        siguiente = null;
    }

 // ================= GETTERS Y SETTERS =================
    public CartaEvento GetCarta() => carta;
    public NodoCarta GetSiguiente() => siguiente;
    public void SetSiguiente(NodoCarta siguiente) => this.siguiente = siguiente;

}

class ListaCircularCartas
{
    private NodoCarta? tail;
    private NodoCarta? actual;
    private int size;

    public ListaCircularCartas()
    {
        tail = null;
        actual = null;
        size = 0;

    }

    // ================= GETTERS Y SETTERS =================
    public NodoCarta getTail() { return tail; }
    public NodoCarta getActual() { return actual; }
    public int getSize() { return size; }
    public void setTail(NodoCarta newTail) {tail = newTail;}
    public void setActual(NodoCarta newActual) { actual = newActual; }


    //metodos
    public void agregarCarta(CartaEvento nodoCarta)
    {
        NodoCarta nuevoNodo = new NodoCarta(nodoCarta);

        if (tail == null)
        {
            setActual(nuevoNodo);
            setTail(nuevoNodo);
            nuevoNodo.SetSiguiente(nuevoNodo);
        }
        else
        {
            nuevoNodo.SetSiguiente(tail.GetSiguiente());
            setTail(nuevoNodo);
    
        }
        size++;
    }
    public CartaEvento tomarCarta()
    {
        if (actual == null)
        {
            return null;   //si la lista esta vacia no retorna nada
        }

        CartaEvento cartaTomada = actual.GetCarta();

        // La carta usada queda al "fondo" al desplazar la referencia actual a la siguiente
        actual = actual.GetSiguiente();

        return cartaTomada;
    }
}

class MazoCartas{
public static ListaCircularCartas CrearMazoCartas()
    {
        ListaCircularCartas mazo = new ListaCircularCartas();

        // ganar dinero, se harán varias cambiando el motivo
        mazo.agregarCarta(new CartaEvento(
            1, 
            "Recibes $50 de regalo.", 
            j => j.setSaldo(j.getSaldo() + 50)
        ));

        // pierde dinero, se harán varias cambiando el motivo
        mazo.agregarCarta(new CartaEvento(
            2, 
            "Paga la tasa escolar de $150.", 
            j => j.setSaldo(j.getSaldo() - 150)
        ));

        // va al inicio del tablero
        mazo.agregarCarta(new CartaEvento(
            3, 
            "Avanza hasta la Salida. Cobra $200.", 
            j => 
            {
                //j.setNodoActual(); debe completarse cuando se defina la casilla inicial
                j.setSaldo(j.getSaldo() + 200);
            }
        ));


        //retrocede 3 casillas
        mazo.agregarCarta(new CartaEvento(
            4, 
            "Retrocede 3 casillas.", 
            j => j.setNodoActual(j.getNodoActual().getAnterior().getAnterior().getAnterior()) //de forma proviciona
        ));

        //avanza 3 casillas
        mazo.agregarCarta(new CartaEvento(
            5, 
            "Avanza 3 casillas.", 
            j => j.setNodoActual(j.getNodoActual().getSiguiente().getSiguiente().getSiguiente()) //de forma proviciona
        ));


        //añadir enviar al inicio y enviar a la carcel cuando estén referenciadas
        return mazo;
    }
}
    