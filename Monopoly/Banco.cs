using System;

namespace Monopoly;

/// <summary>
/// Representa la entidad financiera oficial del Monopoly (Sección 3 y 15).
/// Valida y ejecuta operaciones financieras entre los jugadores y el banco.
/// </summary>
public class Banco
{
    public const string NOMBRE_BANCO = "BANCO";
    public const int PREMIO_SALIDA = 200;

    private int _fondosTotales;

    public int FondosTotales => _fondosTotales;

    public Banco(int fondosIniciales = 100000)
    {
        _fondosTotales = fondosIniciales;
    }

    /// <summary>
    /// Paga el premio oficial por cruzar o caer en la casilla de Salida.
    /// </summary>
    public bool PagarPremioSalida(Jugador jugador, HistorialTransacciones historial, int numeroTurno)
    {
        if (jugador == null) return false;

        jugador.setSaldo(jugador.getSaldo() + PREMIO_SALIDA);
        _fondosTotales -= PREMIO_SALIDA;

        historial?.Registrar(new Transaccion(
            numeroTurno,
            "PREMIO_SALIDA",
            NOMBRE_BANCO,
            jugador.getNombre(),
            PREMIO_SALIDA,
            $"{jugador.getNombre()} recibió ₡{PREMIO_SALIDA} por cruzar o caer en la Salida."
        ));

        return true;
    }

    /// <summary>
    /// Procesa la compra de una propiedad que pertenece al banco.
    /// </summary>
    public bool CobrarCompraPropiedad(Jugador comprador, Propiedad propiedad, HistorialTransacciones historial, int numeroTurno)
    {
        if (comprador == null || propiedad == null) return false;
        if (propiedad.tieneDuenio()) return false;
        if (comprador.getSaldo() < propiedad.getPrecioDeCompra()) return false;

        bool ok = propiedad.comprar(comprador);
        if (ok)
        {
            _fondosTotales += propiedad.getPrecioDeCompra();
            historial?.Registrar(new Transaccion(
                numeroTurno,
                "COMPRA_PROPIEDAD",
                comprador.getNombre(),
                NOMBRE_BANCO,
                propiedad.getPrecioDeCompra(),
                $"{comprador.getNombre()} compró [{propiedad.getIdCasilla()}] {propiedad.getNombre()} por ₡{propiedad.getPrecioDeCompra()}."
            ));
        }
        return ok;
    }

    /// <summary>
    /// Procesa la hipoteca de una propiedad: el banco entrega al jugador el 50% de su valor.
    /// </summary>
    public bool HipotecarPropiedad(Jugador jugador, Propiedad propiedad, HistorialTransacciones historial, int numeroTurno)
    {
        if (jugador == null || propiedad == null) return false;
        if (propiedad.getDuenio() != jugador || propiedad.getEstaHipotecada()) return false;

        int valorHipoteca = propiedad.getPrecioDeCompra() / 2;
        bool ok = propiedad.hipotecar();
        if (ok)
        {
            _fondosTotales -= valorHipoteca;
            historial?.Registrar(new Transaccion(
                numeroTurno,
                "HIPOTECA",
                NOMBRE_BANCO,
                jugador.getNombre(),
                valorHipoteca,
                $"{jugador.getNombre()} hipotecó [{propiedad.getIdCasilla()}] {propiedad.getNombre()} y recibió ₡{valorHipoteca} del banco."
            ));
        }
        return ok;
    }

    /// <summary>
    /// Procesa el pago de deshipoteca: el jugador paga al banco el 50% + 10% de interés.
    /// </summary>
    public bool DeshipotecarPropiedad(Jugador jugador, Propiedad propiedad, HistorialTransacciones historial, int numeroTurno)
    {
        if (jugador == null || propiedad == null) return false;
        if (propiedad.getDuenio() != jugador || !propiedad.getEstaHipotecada()) return false;

        int costo = (propiedad.getPrecioDeCompra() / 2) + (propiedad.getPrecioDeCompra() / 10);
        if (jugador.getSaldo() < costo) return false;

        bool ok = propiedad.desHipotecar();
        if (ok)
        {
            _fondosTotales += costo;
            historial?.Registrar(new Transaccion(
                numeroTurno,
                "DESHIPOTECA",
                jugador.getNombre(),
                NOMBRE_BANCO,
                costo,
                $"{jugador.getNombre()} deshipotecó [{propiedad.getIdCasilla()}] {propiedad.getNombre()} pagando ₡{costo} al banco."
            ));
        }
        return ok;
    }

    /// <summary>
    /// Cobra un monto al jugador con destino al banco (impuestos, multas o cartas de evento).
    /// </summary>
    public bool CobrarAlBanco(Jugador jugador, int monto, string concepto, HistorialTransacciones historial, int numeroTurno)
    {
        if (jugador == null || monto <= 0) return false;

        int aCobrar = Math.Min(jugador.getSaldo(), monto);
        jugador.setSaldo(jugador.getSaldo() - aCobrar);
        _fondosTotales += aCobrar;

        historial?.Registrar(new Transaccion(
            numeroTurno,
            "PAGO_AL_BANCO",
            jugador.getNombre(),
            NOMBRE_BANCO,
            aCobrar,
            $"{jugador.getNombre()} pagó ₡{aCobrar} al banco por concepto de: {concepto}."
        ));

        if (jugador.getSaldo() == 0 && aCobrar < monto)
        {
            jugador.setActivo(false);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Paga un monto del banco al jugador (premios o reembolsos de cartas de evento).
    /// </summary>
    public void PagarDesdeBanco(Jugador jugador, int monto, string concepto, HistorialTransacciones historial, int numeroTurno)
    {
        if (jugador == null || monto <= 0) return;

        jugador.setSaldo(jugador.getSaldo() + monto);
        _fondosTotales -= monto;

        historial?.Registrar(new Transaccion(
            numeroTurno,
            "GANANCIA_EVENTO",
            NOMBRE_BANCO,
            jugador.getNombre(),
            monto,
            $"{jugador.getNombre()} recibió ₡{monto} del banco por concepto de: {concepto}."
        ));
    }
}
