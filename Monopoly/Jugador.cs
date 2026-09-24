
using System.Dynamic;

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
    private bool estaEncarcelado; 
    private bool bancarrotaInminente;
    private bool enBancarrota;
    public Jugador(int newID, string newNombre, NodoCasilla nodoInicio)
    {
        ID = newID;
        nombre = newNombre;
        saldo = 200;
        turnosPerdidos = 0;
        activo = true;
        nodoActual = nodoInicio;
        propiedades = new ListaPropiedades();
        estaEncarcelado = false;
        enBancarrota = false;
        bancarrotaInminente = false;
    }

    // ================= GETTERS Y SETTERS =================
    public int getID() { return ID; }
    public string getNombre() { return nombre; }
    public int getSaldo() { return saldo; }
    public int getTurnosPerdidos() { return turnosPerdidos; }
    public bool getEstaEncarcelado() {return estaEncarcelado;}
    public bool isActivo() { return activo; }
    public NodoCasilla getNodoActual() { return nodoActual; }
    public ListaPropiedades getPropiedades() {return propiedades; }
    public bool getEnBancarrota() {return enBancarrota; }
    public bool getBancarrotaInminente() {return bancarrotaInminente; }
    

    public void setSaldo(int newSaldo) { saldo = newSaldo; }
    public void setTurnosPerdidos(int newTurnos) { turnosPerdidos = newTurnos; }
    public void setActivo(bool newActivo) { activo = newActivo; }
    public void setNodoActual(NodoCasilla newNodo) { nodoActual = newNodo; }
    public void setEstaEncarcelado(bool estado) { estaEncarcelado = estado; }
    public void setEnBancarrota(bool newValue) {enBancarrota = newValue; }
    public void setBancarrotaInminente(bool newValue){bancarrotaInminente = newValue; }


    //Métodos

}

// in the deepest ocean... 
// the bottom of the sea...
// your eyes...
//  they turn me...