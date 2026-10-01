using System;

namespace Monopoly;

/// <summary>
/// Clase principal del modelo de juego Monopoly (Sección 15 y 18).
/// Centraliza las estructuras de datos principales (Tablero, Turnos, Banco, Transacciones, Dado)
/// y evalúa las condiciones de victoria y fin de partida.
/// </summary>
public class Juego
{
    public Tablero Tablero { get; }
    public ListaTurnos Turnos { get; }
    public Banco Banco { get; }
    public HistorialTransacciones Historial { get; }
    public Dado Dado { get; }

    public int MaxTurnos { get; set; }
    public int ContadorTurnosGlobal { get; private set; }
    public bool PartidaFinalizada { get; private set; }
    public string? Ganador { get; private set; }
    public string? MotivoFinPartida { get; private set; }

    public Juego(int maxTurnos = 100, string rutaTransacciones = "transacciones.txt")
    {
        Tablero = new Tablero();
        Turnos = new ListaTurnos();
        Banco = new Banco();
        Historial = new HistorialTransacciones(rutaTransacciones);
        Dado = new Dado();

        MaxTurnos = maxTurnos;
        ContadorTurnosGlobal = 0;
        PartidaFinalizada = false;
        Ganador = null;
        MotivoFinPartida = null;
    }

    /// <summary>
    /// Calcula el patrimonio oficial de un jugador (Sección 18: saldo + valor de propiedades).
    /// </summary>
    public int CalcularPatrimonio(Jugador jugador)
    {
        if (jugador == null) return 0;
        int saldo = Math.Max(0, jugador.getSaldo());
        int valorPropiedades = jugador.getPropiedades().calcularValorTotal();
        return saldo + valorPropiedades;
    }

    /// <summary>
    /// Incrementa el contador global de turnos y evalúa condiciones de término (Sección 18).
    /// </summary>
    public bool RegistrarFinDeTurno()
    {
        ContadorTurnosGlobal++;
        return EvaluarFinDePartida();
    }

    /// <summary>
    /// Evalúa si la partida finaliza por:
    /// 1. Queda un único jugador activo.
    /// 2. Se alcanzó la cantidad máxima configurable de turnos (gana el de mayor patrimonio).
    /// </summary>
    public bool EvaluarFinDePartida()
    {
        if (PartidaFinalizada) return true;

        int activos = Turnos.CantidadJugadoresActivos();
        int total = Turnos.GetTotalJugadores();

        // Condición 1: Único sobreviviente
        if (total > 1 && activos == 1)
        {
            var ganador = Turnos.avanzarTurno();
            if (ganador != null)
            {
                PartidaFinalizada = true;
                Ganador = ganador.getNombre();
                MotivoFinPartida = $"Único jugador activo restante (todos los demás en bancarrota).";
                return true;
            }
        }

        // Condición 2: Límite de turnos alcanzado
        if (MaxTurnos > 0 && ContadorTurnosGlobal >= MaxTurnos && total > 0)
        {
            PartidaFinalizada = true;
            DeterminarGanadorPorPatrimonio();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determina el ganador comparando el patrimonio (saldo + valor de propiedades) entre todos los jugadores activos.
    /// </summary>
    private void DeterminarGanadorPorPatrimonio()
    {
        var primero = Turnos.GetActualNodo();
        if (primero == null) return;

        var actual = primero;
        Jugador? mejorJugador = null;
        int maxPatrimonio = -1;

        int visitados = 0;
        int total = Turnos.GetTotalJugadores();

        while (actual != null && visitados < total)
        {
            var j = actual.getJugador();
            if (j.isActivo())
            {
                int pat = CalcularPatrimonio(j);
                if (pat > maxPatrimonio)
                {
                    maxPatrimonio = pat;
                    mejorJugador = j;
                }
            }
            actual = actual.getSiguiente();
            visitados++;
        }

        if (mejorJugador != null)
        {
            Ganador = mejorJugador.getNombre();
            MotivoFinPartida = $"Límite de {MaxTurnos} turnos alcanzado. Mayor patrimonio acumulado: ₡{maxPatrimonio} ({mejorJugador.getNombre()}).";
        }
    }
}
