namespace Monopoly;

class Banco
{
    private Tablero TableroJuego;
    private ListaCircularCartas MazoEventos;
    private ListaTurnos TurnosJugadores;


    public  Banco()
    {
        TableroJuego = new Tablero();
        MazoEventos = new ListaCircularCartas();
        TurnosJugadores = new ListaTurnos();
    }

    private void poblarTablero(Tablero tablero)
    {
        //Aqui es donde vamos a creear el tablero
    }
}