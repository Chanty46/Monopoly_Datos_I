namespace Monopoly;

class Banco
{
    private Tablero TableroJuego;
    private ListaCircularCartas MazoEventos;
    private ListaTurnos TurnosJugadores;
    private Random random;


    public  Banco()
    {
        TableroJuego = new Tablero();
        MazoEventos = new ListaCircularCartas();
        TurnosJugadores = new ListaTurnos();
        random = new Random();
    }

    private void poblarTablero(Tablero tablero)
    {
        //Aqui es donde vamos a crear el tablero
    }

    public void RegistrarJugador(Jugador jugador) //introduce jugador a la lista de turnos
    {
        if (jugador != null)
        {
            TurnosJugadores.agregarJugador(jugador);
        }
    }
    public int LanzarDados()  //lanza dados, xd
    {
        int dado1 = random.Next(1, 7);
        int dado2 = random.Next(1, 7);
        return dado1 + dado2;
    }

   public void ProcesarTurnoActual()
    {
        if (TurnosJugadores.getCantidadJugadores() == 0) return;
        Jugador jugadorActual = TurnosJugadores.getTurnoActual();
        if (jugadorActual == null) return; //se asegura de que exista el jugador

        // verifica si está en la cárcel
        if (jugadorActual.getEstaEncarcelado())
        {
            if (jugadorActual.getTurnosPerdidos() > 0)
            {
                //reduce los turnos restantes en la cárcel
                jugadorActual.setTurnosPerdidos(jugadorActual.getTurnosPerdidos() - 1);

                if (jugadorActual.getTurnosPerdidos() == 0)
                {
                    jugadorActual.setEstaEncarcelado(false);
                }

                TurnosJugadores.avanzarTurno();
                return; //pierde el movimiento de este turno y se avanza en la lista circular
            }
        }

        // moverse en el tablero
        int pasos = LanzarDados();        
        TableroJuego.moverJugadorPorDados(jugadorActual, pasos);


        // evalúa saldo para bancarrota
        if (jugadorActual.getSaldo() <= 0)
        {
            TurnosJugadores.eliminarJugadorBancarrota(jugadorActual);
        }
        else
        {
            TurnosJugadores.avanzarTurno();
        }
    
    }
}