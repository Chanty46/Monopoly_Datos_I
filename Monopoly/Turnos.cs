using System;

namespace Monopoly;

public class NodoTurno
{
    private Jugador jugador;
    private int dadosPasos;
    private NodoTurno? siguiente;

    public NodoTurno(Jugador jugador)
    {
        this.jugador = jugador;
        dadosPasos = 0;
        siguiente = null;
    }

    // Getters y Setters
    public Jugador getJugador() => jugador;
    public int getDadosPasos() => dadosPasos;
    public NodoTurno? getSiguiente() => siguiente;

    public void setDadosPasos(int pasos) => dadosPasos = pasos;
    public void setSiguiente(NodoTurno? siguiente) => this.siguiente = siguiente;
}

// Estructura propia: Lista Circular Simplemente Enlazada para la rotación de turnos
public class ListaTurnos
{
    private NodoTurno? actual; // Apunta al jugador que tiene el turno
    private NodoTurno? tail;   // Mantiene la estructura circular
    private int totalJugadores;
    private int numeroRonda;

    public ListaTurnos()
    {
        actual = null;
        tail = null;
        totalJugadores = 0;
        numeroRonda = 1;
    }

    public int GetTotalJugadores() => totalJugadores;
    public int GetNumeroRonda() => numeroRonda;
    public NodoTurno? GetActualNodo() => actual;

    public void agregarJugador(Jugador jugador)
    {
        var nuevoNodo = new NodoTurno(jugador);
        if (tail == null)
        {
            tail = nuevoNodo;
            tail.setSiguiente(tail); // Se enlaza a sí mismo
            actual = tail;
        }
        else
        {
            nuevoNodo.setSiguiente(tail.getSiguiente());
            tail.setSiguiente(nuevoNodo);
            tail = nuevoNodo;
        }
        totalJugadores++;
    }

    // Retorna qué jugador tiene el turno actual
    public Jugador? getTurnoActual()
    {
        if (actual == null) return null;
        return actual.getJugador();
    }

    // Avanza el turno al siguiente jugador activo en la lista circular
    public Jugador? avanzarTurno()
    {
        if (actual == null || totalJugadores == 0) return null;

        int intentos = 0;
        do
        {
            // Si el siguiente nodo es el inicio de la lista (siguiente de tail), incrementa ronda
            if (actual == tail)
            {
                numeroRonda++;
            }

            actual = actual.getSiguiente();
            intentos++;

            // Si el jugador está activo, este es su turno
            if (actual!.getJugador().isActivo())
            {
                return actual.getJugador();
            }

        } while (intentos <= totalJugadores);

        // Si nadie está activo, retornar el actual
        return actual?.getJugador();
    }

    public int CantidadJugadoresActivos()
    {
        if (tail == null) return 0;
        int activos = 0;
        var temp = tail.getSiguiente();
        for (int i = 0; i < totalJugadores; i++)
        {
            if (temp != null && temp.getJugador().isActivo()) activos++;
            temp = temp?.getSiguiente();
        }
        return activos;
    }

    public Jugador? BuscarPorId(int id)
    {
        if (tail == null) return null;
        var temp = tail.getSiguiente();
        for (int i = 0; i < totalJugadores; i++)
        {
            if (temp != null && temp.getJugador().getID() == id)
            {
                return temp.getJugador();
            }
            temp = temp?.getSiguiente();
        }
        return null;
    }

    public Jugador? BuscarPorRfid(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid) || tail == null) return null;
        var temp = tail.getSiguiente();
        for (int i = 0; i < totalJugadores; i++)
        {
            if (temp != null && string.Equals(temp.getJugador().getRfidUid(), uid, StringComparison.OrdinalIgnoreCase))
            {
                return temp.getJugador();
            }
            temp = temp?.getSiguiente();
        }
        return null;
    }
}
