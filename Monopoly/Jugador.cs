
namespace Monopoly;
class Jugador
{
    private int identificador;
    private string nombre;
    private int saldo;
    private int posicion; // seria como el identificador de casilla, en este caso el ID de la casilla nos dira donde esta parado
    private int turnosPerdidos;
    private bool activo;

    // definir : Lista de propiedades

    public Jugador(int newID, string newNombre)
    {
        identificador = newID;
        nombre = newNombre;
        saldo = 200;
        turnosPerdidos = 0;
        activo = false;
        posicion = 0; 
    }
}

