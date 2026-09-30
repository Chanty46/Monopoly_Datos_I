using System;

namespace Monopoly;

public class Jugador
{
    private int ID;
    private string nombre;
    private int saldo;
    private int turnosPerdidos;
    private bool activo;
    private NodoCasilla? nodoActual; // Única referencia necesaria en el tablero circular
    private ListaPropiedades propiedades;
    private string? rfidUid; // Identificador RFID asociado opcionalmente

    public Jugador(int newID, string newNombre, NodoCasilla? nodoInicio, int saldoInicial = 1500)
    {
        ID = newID;
        nombre = newNombre;
        saldo = saldoInicial;
        turnosPerdidos = 0;
        activo = true;
        nodoActual = nodoInicio;
        propiedades = new ListaPropiedades();
        rfidUid = null;
    }

    // ================= GETTERS Y SETTERS =================
    public int getID() => ID;
    public string getNombre() => nombre;
    public int getSaldo() => saldo;
    public int getTurnosPerdidos() => turnosPerdidos;
    public bool isActivo() => activo;
    public NodoCasilla? getNodoActual() => nodoActual;
    public ListaPropiedades getPropiedades() => propiedades;
    public string? getRfidUid() => rfidUid;

    public void setSaldo(int newSaldo) => saldo = Math.Max(0, newSaldo);
    public void setTurnosPerdidos(int newTurnos) => turnosPerdidos = Math.Max(0, newTurnos);
    public void setActivo(bool newActivo) => activo = newActivo;
    public void setNodoActual(NodoCasilla? newNodo) => nodoActual = newNodo;
    public void setRfidUid(string? uid) => rfidUid = uid;

    public bool isEncarcelado() => (turnosPerdidos > 0);

    public void agregarSaldo(int monto)
    {
        if (monto > 0) saldo += monto;
    }

    public bool descontarSaldo(int monto)
    {
        if (monto <= saldo)
        {
            saldo -= monto;
            return true;
        }
        saldo = 0;
        activo = false; // Bancarrota
        return false;
    }
}

// in the deepest ocean... 
// the bottom of the sea...
// your eyes, they turn me...