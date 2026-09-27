using System.Runtime.CompilerServices;

namespace Monopoly;
// Aqui definimos lo que es el tablero, que tiene muchas cosas por hacer pero toca jiji
class NodoCasilla
{
    private Casilla casilla;
    private NodoCasilla? siguiente;
    private NodoCasilla? anterior;

    public NodoCasilla(Casilla newCasilla)
    {
        casilla = newCasilla;
        siguiente = null;
        anterior = null;
    }
    //Getters 
    public Casilla getCasilla() { return casilla; }
    public NodoCasilla getSiguiente() { return siguiente!; }
    public NodoCasilla getAnterior() { return anterior!; }

    //Setters
    public void setSiguiente(NodoCasilla? newSiguiente) { siguiente = newSiguiente; }
    public void setAnterior(NodoCasilla? newAnterior) { anterior = newAnterior; }
}

class Tablero
{
    private NodoCasilla? head; //Sera el inicio.
    private NodoCasilla? tail; 
    
    private int casasRestantes; //Aumentan el valor de la renta
    private int hotelesRestantes; //Aumentan el valor de la renta

    //Getters 
    public NodoCasilla getHead() { return head!; }
    public NodoCasilla getTail() { return tail!; }
    public int getCasasRestantes() { return casasRestantes; }
    public int getHotelesRestantes() { return hotelesRestantes; }

    //Setters 
    public void setHead(NodoCasilla? head) { this.head = head; }
    public void setTail(NodoCasilla? tail) { this.tail = tail; }
    public void setCasasRestantes(int casas) { casasRestantes = casas; }
    public void setHotelesRestantes(int hoteles) { hotelesRestantes = hoteles; }
   
    public Tablero()
    {
        head = null;
        tail = null;
        casasRestantes = 32;
        hotelesRestantes = 12;
        // De acuerdo a la IA, Monopoly incluye 32 casas y 12 hoteles
    }

    public void agregarCasilla(Casilla newCasilla) // esta es mas que todo para poblar el talbero
    {
        NodoCasilla nuevoNodo = new NodoCasilla(newCasilla);

        if (head == null)
        {
            head = nuevoNodo; 
            tail = nuevoNodo;
            head.setSiguiente(head);
            head.setAnterior(head);
        } else {
            tail!.setSiguiente(nuevoNodo);
            nuevoNodo.setAnterior(tail);
            nuevoNodo.setSiguiente(head);
            head.setAnterior(nuevoNodo);
            tail = nuevoNodo;
        }
    } 

    // Para efectos de este proyecto, no se ocupa remover ni insertar en medio de una casilla, ya que el tablero es fijo, como mencione, solo sirve el agregarCasilla
    // Luego seria buscarPorID que el id es la posicion en la que la casilla se encuentra
    // esto nos sirve para "teledirigir" a nuestros jugadores directo a una casilla sahur

    public NodoCasilla? buscarCasillaPorID(int id)
    { 
        if (head == null) { return null; }

        NodoCasilla actual = head; 
        bool primeraVez = true;
        while (actual != head || primeraVez)
        {
            primeraVez = false;

            if (actual.getCasilla().getIdCasilla() == id)
            {
                return actual;
            }

            actual = actual.getSiguiente();
        }

        return null;
    }

    public void moverJugadorACasilla(Jugador jugador, int idDestino)
    {
        NodoCasilla? destino = buscarCasillaPorID(idDestino);

        if (destino != null)
        {
            jugador.setNodoActual(destino);
        }
    }

    // Solo mueve al jugador los pasos indicados por los dados.
    // Retorna true si el jugador paso o cayo en Salida (ID 0) durante el movimiento.
    // La reaccion a la casilla destino (aplicarCasilla, encarcelamiento) es responsabilidad del Banco.
    public bool moverJugadorPorDados(Jugador jugador, int pasosDados)
    {
        bool pasoPorSalida = false;

        for (int i = 0; i < pasosDados; i++)
        {
            NodoCasilla siguienteNodo = jugador.getNodoActual().getSiguiente();
            jugador.setNodoActual(siguienteNodo);

            // Detectar si paso o cayo en Salida (head = ID 0)
            if (jugador.getNodoActual() == head)
            {
                pasoPorSalida = true;
            }
        }

        return pasoPorSalida;
    }

    // Teletransporta al jugador a la Carcel (ID 10) si esta marcado como encarcelado
    // y aun no se encuentra fisicamente en ella. El Banco lo llama despues de aplicarCasilla.
    public void fueEncarcelado(Jugador jugador)
    {
        if (jugador.getEstaEncarcelado() && jugador.getNodoActual().getCasilla().getIdCasilla() != 10)
        {
            moverJugadorACasilla(jugador, 10);
        }
    }
}