using System;

namespace Monopoly;

/// <summary>
/// Representa el par de dados electrónicos del juego (Sección 9 y 15).
/// </summary>
public class Dado
{
    private readonly Random _random;
    public int Dado1 { get; private set; }
    public int Dado2 { get; private set; }
    public int Total => Dado1 + Dado2;
    public bool EsDoble => Dado1 == Dado2 && Dado1 > 0;

    public Dado()
    {
        _random = Random.Shared;
        Dado1 = 1;
        Dado2 = 1;
    }

    public Dado(int semilla)
    {
        _random = new Random(semilla);
        Dado1 = 1;
        Dado2 = 1;
    }

    /// <summary>
    /// Lanza los dos dados generando valores entre 1 y 6.
    /// </summary>
    public (int d1, int d2, int total) Lanzar()
    {
        Dado1 = _random.Next(1, 7);
        Dado2 = _random.Next(1, 7);
        return (Dado1, Dado2, Total);
    }

    /// <summary>
    /// Permite fijar los valores de los dados (útil para pruebas controladas o dados físicos externos).
    /// </summary>
    public void FijarValores(int d1, int d2)
    {
        if (d1 < 1 || d1 > 6 || d2 < 1 || d2 > 6)
            throw new ArgumentOutOfRangeException("Los valores del dado deben estar entre 1 y 6.");
        Dado1 = d1;
        Dado2 = d2;
    }
}
