
namespace Monopoly;

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
}

class Tablero
{
    private NodoCasilla head; //Sera el inicio.
    private NodoCasilla tail; 
    private int casasRestantes; //Aumentan el valor de la renta
    private int hotelesRestantes; //Aumentan el valor de la renta
}