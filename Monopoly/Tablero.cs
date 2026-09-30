using System;

namespace Monopoly;

public class NodoCasilla
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

    // Getters 
    public Casilla getCasilla() => casilla;
    public NodoCasilla? getSiguiente() => siguiente;
    public NodoCasilla? getAnterior() => anterior;

    // Setters
    public void setSiguiente(NodoCasilla? newSiguiente) => siguiente = newSiguiente;
    public void setAnterior(NodoCasilla? newAnterior) => anterior = newAnterior;
}

public class Tablero
{
    private NodoCasilla? head; // Inicio del tablero circular
    private NodoCasilla? tail;
    private int casasRestantes;
    private int hotelesRestantes;
    private int totalCasillas;
    public MazoEventos MazoEventos { get; }

    public NodoCasilla? getHead() => head;
    public NodoCasilla? getTail() => tail;
    public int getCasasRestantes() => casasRestantes;
    public int getHotelesRestantes() => hotelesRestantes;
    public int getTotalCasillas() => totalCasillas;

    public void setHead(NodoCasilla? head) => this.head = head;
    public void setTail(NodoCasilla? tail) => this.tail = tail;
    public void setCasasRestantes(int casas) => casasRestantes = casas;
    public void setHotelesRestantes(int hoteles) => hotelesRestantes = hoteles;

    public Tablero()
    {
        head = null;
        tail = null;
        casasRestantes = 32;
        hotelesRestantes = 12;
        totalCasillas = 0;
        MazoEventos = new MazoEventos();
    }

    // Inserción en Lista Circular Doblemente Enlazada
    public void agregarCasilla(Casilla newCasilla)
    {
        var nuevoNodo = new NodoCasilla(newCasilla);

        if (head == null)
        {
            head = nuevoNodo;
            tail = nuevoNodo;
            head.setSiguiente(head);
            head.setAnterior(head);
        }
        else
        {
            tail!.setSiguiente(nuevoNodo);
            nuevoNodo.setAnterior(tail);
            nuevoNodo.setSiguiente(head);
            head.setAnterior(nuevoNodo);
            tail = nuevoNodo;
        }
        totalCasillas++;
    }

    public NodoCasilla? buscarCasillaPorID(int id)
    {
        if (head == null) return null;

        NodoCasilla actual = head;
        do
        {
            if (actual.getCasilla().getIdCasilla() == id)
            {
                return actual;
            }
            actual = actual.getSiguiente()!;
        } while (actual != head);

        return null;
    }

    public void moverJugadorACasilla(Jugador jugador, int idDestino)
    {
        var destino = buscarCasillaPorID(idDestino);
        if (destino != null)
        {
            jugador.setNodoActual(destino);
        }
    }

    // Mueve al jugador paso a paso en la lista circular doble.
    // Detecta si pasa por la salida (Casilla 0) para cobrar ₡200.
    public Casilla moverJugadorPorDados(Jugador jugador, int pasos, out bool pasoPorSalida)
    {
        pasoPorSalida = false;
        if (jugador.getNodoActual() == null)
        {
            jugador.setNodoActual(head);
        }

        var actual = jugador.getNodoActual()!;

        for (int i = 0; i < pasos; i++)
        {
            actual = actual.getSiguiente()!;
            // Si durante el trayecto pasa por la salida (Casilla 0), cobra el bono
            if (actual.getCasilla().getIdCasilla() == 0 && i < pasos - 1)
            {
                pasoPorSalida = true;
                jugador.agregarSaldo(200);
            }
        }

        jugador.setNodoActual(actual);
        return actual.getCasilla();
    }

    // Inicializa el tablero oficial de 24 casillas (0 a 23)
    public void InicializarTablero24()
    {
        // 0: Salida
        agregarCasilla(new CasillaInicial("Salida (GO)", 0));
        
        // 1-2: Café
        agregarCasilla(new Propiedad("Avenida Central", 1, 60, 10, "Café"));
        agregarCasilla(new Propiedad("Avenida Segunda", 2, 60, 10, "Café"));
        
        // 3: Evento
        agregarCasilla(new CasillaEvento("Suerte / Arca 1", 3));
        
        // 4-5: Celeste
        agregarCasilla(new Propiedad("Paseo Colón", 4, 100, 15, "Celeste"));
        agregarCasilla(new Propiedad("Calle Real", 5, 120, 20, "Celeste"));
        
        // 6: Cárcel
        agregarCasilla(new CasillaCarcel("Cárcel", 6));
        
        // 7-8: Rosa
        agregarCasilla(new Propiedad("Barrio Escalante", 7, 140, 25, "Rosa"));
        agregarCasilla(new Propiedad("Barrio Amón", 8, 160, 30, "Rosa"));
        
        // 9: Evento
        agregarCasilla(new CasillaEvento("Suerte / Arca 2", 9));
        
        // 10-11: Naranja
        agregarCasilla(new Propiedad("Los Yoses", 10, 180, 35, "Naranja"));
        agregarCasilla(new Propiedad("San Pedro", 11, 200, 40, "Naranja"));
        
        // 12: Parqueo Libre
        agregarCasilla(new CasillaParqueoLibre("Parqueo Libre", 12));
        
        // 13-14: Rojo
        agregarCasilla(new Propiedad("La Sabana", 13, 220, 45, "Rojo"));
        agregarCasilla(new Propiedad("Rohrmoser", 14, 240, 50, "Rojo"));
        
        // 15: Evento
        agregarCasilla(new CasillaEvento("Suerte / Arca 3", 15));
        
        // 16-17: Amarillo
        agregarCasilla(new Propiedad("Curridabat", 16, 260, 55, "Amarillo"));
        agregarCasilla(new Propiedad("Escazú", 17, 280, 60, "Amarillo"));
        
        // 18: Vaya a la Cárcel
        agregarCasilla(new CasillaPolicia("Vaya a la Cárcel (Policía)", 18));
        
        // 19-20: Verde
        agregarCasilla(new Propiedad("Santa Ana", 19, 300, 65, "Verde"));
        agregarCasilla(new Propiedad("Heredia Centro", 20, 320, 70, "Verde"));
        
        // 21: Evento
        agregarCasilla(new CasillaEvento("Suerte / Arca 4", 21));
        
        // 22-23: Azul Oscuro
        agregarCasilla(new Propiedad("Pinares", 22, 350, 80, "Azul"));
        agregarCasilla(new Propiedad("Montealegre", 23, 400, 100, "Azul"));
    }
}
