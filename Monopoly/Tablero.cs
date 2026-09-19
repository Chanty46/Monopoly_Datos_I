using System.Runtime.CompilerServices;

namespace Monopoly;
// Aqui definimos lo que es el tablero, que tiene muchas cosas por hacer pero toca jiji
class NodoCasilla
{
    private Casilla casilla;
    private NodoCasilla siguiente;
    private NodoCasilla anterior;
 

    public NodoCasilla(Casilla newCasilla)
    {
        casilla = newCasilla;
        siguiente = null;
        anterior = null;
    }
    //Getters 
    public Casilla getCasilla() { return casilla; }
    public NodoCasilla getSiguiente() { return siguiente; }
    public NodoCasilla getAnterior() { return anterior; }

    //Setters
    public void setSiguiente(NodoCasilla newSiguiente) { siguiente = newSiguiente; }
    public void setAnterior(NodoCasilla newAnterior) { anterior = newAnterior; }
}

class Tablero
{
    private NodoCasilla head; //Sera el inicio.
    private NodoCasilla tail; 
    
    private int casasRestantes; //Aumentan el valor de la renta
    private int hotelesRestantes; //Aumentan el valor de la renta

    //Getters 
    public NodoCasilla getHead() { return head; }
    public NodoCasilla getTail() { return tail; }
    public int getCasasRestantes() { return casasRestantes; }
    public int getHotelesRestantes() { return hotelesRestantes; }

    //Setters 
    public void setHead(NodoCasilla head) { this.head = head; }
    public void setTail(NodoCasilla tail) { this.tail = tail; }
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
            tail.setSiguiente(nuevoNodo);
            nuevoNodo.setAnterior(tail);
            nuevoNodo.setSiguiente(head);
            head.setAnterior(nuevoNodo);
            tail = nuevoNodo;
        }
    } 

    // Para efectos de este proyecto, no se ocupa remover ni insertar en medio de una casilla, ya que el tablero es fijo, como mencione, solo sirve el agregarCasilla
    // Luego seria buscarPorID que el id es la posicion en la que la casilla se encuentra
    // esto nos sirve para "teledirigir" a nuestros jugadores directo a una casilla sahur

    public NodoCasilla buscarCasillaPorID(int id)
    { if (head == null) {return null;}

    NodoCasilla actual = head; 
    bool primeraVez = true;
        while (actual != head || primeraVez) // basicamente ver esto es para recorrer todo el tablero Y encontrar una casilla, y hay que crear un bool para saber si ya pasamos por head
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
    NodoCasilla destino = buscarCasillaPorID(idDestino); //Como solo guardamos los ID, aqui es importante mover al jugador.

    if (destino != null)
    {
        jugador.setNodoActual(destino);
    }
    }

    public void moverJugadorPorDados(Jugador jugador, int pasosDados)
    { for (int i = 0; i < pasosDados; i++) //Ciclo for donde nos movemos de uno a uno hasta alcanzar la cantidad de pasos dados
        {
        // Avanzamos al siguiente nodo y actualizamos la referencia del jugador inmediatamente
        NodoCasilla siguienteNodo = jugador.getNodoActual().getSiguiente(); //Es decir, el algoritmo primero reconoce cual es el siguiente paso a dar y lo da.
        jugador.setNodoActual(siguienteNodo);

        //Revisar si el paso/cayo en la principal
        if (jugador.getNodoActual() == getHead())
            {
            jugador.getNodoActual().getCasilla().aplicarCasilla(jugador); //Del nodo obtendriamos la casilla de salida, y esta misma aplica el metodo al jugador
            }
        }

    // Al terminar todos los pasos, ejecutamos la acción de la casilla final
    jugador.getNodoActual().getCasilla().aplicarCasilla(jugador);

    // Revisar si el jugador fue encarcelado
    fueEncarcelado(jugador); //Si fue encarcelado, enviar a la chorpa
    }

    public void fueEncarcelado(Jugador jugador)
    {                                                                                           //Es decir NO esta en la carcel:
       if (jugador.getEstaEncarcelado() && jugador.getNodoActual().getCasilla().getIdCasilla() != 1)
        {
            moverJugadorACasilla(jugador, 1); //suponiendo que el uno es la carcel
        }
    }

}
