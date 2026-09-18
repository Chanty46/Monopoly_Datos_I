
namespace Monopoly;
class Jugador
{
    private int ID;
    private string nombre;
    private int saldo;
    private int turnosPerdidos;
    private bool activo;
    private NodoCasilla nodoActual; // Única referencia necesaria
    private ListaPropiedades propiedades; 

    public Jugador(int newID, string newNombre, NodoCasilla nodoInicio)
    {
        ID = newID;
        nombre = newNombre;
        saldo = 200;
        turnosPerdidos = 0;
        activo = true;
        nodoActual = nodoInicio;
        propiedades = new ListaPropiedades();
    }

    // ================= GETTERS Y SETTERS =================
    public int getID() { return ID; }
    public string getNombre() { return nombre; }
    public int getSaldo() { return saldo; }
    public int getTurnosPerdidos() { return turnosPerdidos; }
    public bool isActivo() { return activo; }
    public NodoCasilla getNodoActual() { return nodoActual; }
    public ListaPropiedades getPropiedades() {return propiedades; }

    public void setSaldo(int newSaldo) { saldo = newSaldo; }
    public void setTurnosPerdidos(int newTurnos) { turnosPerdidos = newTurnos; }
    public void setActivo(bool newActivo) { activo = newActivo; }
    public void setNodoActual(NodoCasilla newNodo) { nodoActual = newNodo; }

    public bool isEncarcelado() { return (turnosPerdidos > 0); }
}

// in the deepest ocean... 
// the bottom of the sea...
// your eyes, they turn me...