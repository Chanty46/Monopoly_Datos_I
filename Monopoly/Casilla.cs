using System;

namespace Monopoly;

public class Casilla
{
    private string nombre;
    private int idCasilla;

    public Casilla(string newNombre, int newID)
    {
        nombre = newNombre;
        idCasilla = newID;
    }

    public string getNombre() { return nombre; }
    public int getIdCasilla() { return idCasilla; }

    public void setNombre(string newNombre) { nombre = newNombre; }
    public void setIdCasilla(int newID) { idCasilla = newID; }

    // Polimorfismo unificado: cada casilla aplica su efecto y retorna el resultado
    public virtual string aplicarCasilla(Jugador jugador, Tablero tablero)
    {
        return $"{jugador.getNombre()} cayó en {nombre} (Casilla {idCasilla}).";
    }
}

public class Propiedad : Casilla
{
    private int precioDeCompra;
    private int alquiler;
    private string grupo;
    private Jugador? duenio;
    private int casasPuestas;
    private bool tieneHotel;
    private bool estaHipotecada;

    public Propiedad(string newNombre, int newID, int newPrecio, int newAlquiler, string newGrupo) 
        : base(newNombre, newID)
    {
        precioDeCompra = newPrecio;
        alquiler = newAlquiler;
        grupo = newGrupo;
        casasPuestas = 0;
        tieneHotel = false;
        estaHipotecada = false;
        duenio = null;
    }

    public int getPrecioDeCompra() { return precioDeCompra; }
    public int getAlquiler() { return alquiler; }
    public string getGrupo() { return grupo; }
    public Jugador? getDuenio() { return duenio; }
    public int getCasasPuestas() { return casasPuestas; }
    public bool getTieneHotel() { return tieneHotel; }
    public bool getEstaHipotecada() { return estaHipotecada; }

    public void setPrecioDeCompra(int newPrecio) { precioDeCompra = newPrecio; }
    public void setAlquiler(int newAlquiler) { alquiler = newAlquiler; }
    public void setGrupo(string newGrupo) { grupo = newGrupo; }
    public void setDuenio(Jugador? newDuenio) { duenio = newDuenio; }
    public void setCasasPuestas(int newCasas) { casasPuestas = newCasas; }
    public void setTieneHotel(bool newTieneHotel) { tieneHotel = newTieneHotel; }
    public void setEstaHipotecada(bool newValue) { estaHipotecada = newValue; }

    public bool tieneDuenio() { return duenio != null; }

    public bool comprar(Jugador comprador)
    {
        if (tieneDuenio()) return false;
        if (comprador.getSaldo() < precioDeCompra) return false;

        duenio = comprador;
        comprador.setSaldo(comprador.getSaldo() - precioDeCompra);
        comprador.getPropiedades().agregarPropiedad(this);
        return true;
    }

    public bool hipotecar()
    {
        if (duenio == null || estaHipotecada) return false;
        if (casasPuestas > 0 || tieneHotel) return false; // No se puede hipotecar con mejoras

        estaHipotecada = true;
        int valorHipoteca = precioDeCompra / 2;
        duenio.setSaldo(duenio.getSaldo() + valorHipoteca);
        return true;
    }

    public bool desHipotecar()
    {
        if (duenio == null || !estaHipotecada) return false;
        int costoDeshipoteca = (precioDeCompra / 2) + (precioDeCompra / 10); // 50% + 10% interes
        if (duenio.getSaldo() < costoDeshipoteca) return false;

        duenio.setSaldo(duenio.getSaldo() - costoDeshipoteca);
        estaHipotecada = false;
        return true;
    }

    public string cobrarAlquiler(Jugador jugador, out int montoCobrado)
    {
        montoCobrado = 0;
        if (duenio == null || duenio == jugador || estaHipotecada)
        {
            return $"{jugador.getNombre()} no debe pagar alquiler.";
        }

        montoCobrado = Math.Min(jugador.getSaldo(), alquiler);
        jugador.setSaldo(jugador.getSaldo() - montoCobrado);
        duenio.setSaldo(duenio.getSaldo() + montoCobrado);

        if (jugador.getSaldo() == 0 && montoCobrado < alquiler)
        {
            jugador.setActivo(false); // Bancarrota
            return $"{jugador.getNombre()} no pudo pagar el alquiler completo de ₡{alquiler} a {duenio.getNombre()} y ha caído en bancarrota.";
        }

        return $"{jugador.getNombre()} pagó ₡{montoCobrado} de alquiler a {duenio.getNombre()}.";
    }

    public override string aplicarCasilla(Jugador jugador, Tablero tablero)
    {
        if (!tieneDuenio())
        {
            return $"PROPIEDAD_DISPONIBLE|{getIdCasilla()}|{getNombre()}|{precioDeCompra}|{alquiler}";
        }

        if (duenio == jugador)
        {
            return $"{jugador.getNombre()} está de visita en su propia propiedad ({getNombre()}).";
        }

        if (estaHipotecada)
        {
            return $"{getNombre()} está hipotecada. {jugador.getNombre()} no paga alquiler.";
        }

        return cobrarAlquiler(jugador, out _);
    }
}

public class CasillaEspecial : Casilla
{
    public CasillaEspecial(string nombre, int id) : base(nombre, id) { }
}

public class CasillaInicial : CasillaEspecial
{
    public CasillaInicial(string nombre, int id) : base(nombre, id) { }

    public override string aplicarCasilla(Jugador jugador, Tablero tablero)
    {
        jugador.setSaldo(jugador.getSaldo() + 200);
        return $"{jugador.getNombre()} cayó exactamente en la Salida y cobra ₡200.";
    }
}

public class CasillaCarcel : CasillaEspecial
{
    public CasillaCarcel(string nombre, int id) : base(nombre, id) { }

    public override string aplicarCasilla(Jugador jugador, Tablero tablero)
    {
        if (jugador.isEncarcelado())
        {
            jugador.setTurnosPerdidos(jugador.getTurnosPerdidos() - 1);
            if (jugador.getTurnosPerdidos() == 0)
            {
                return $"{jugador.getNombre()} ha cumplido su condena y queda libre.";
            }
            return $"{jugador.getNombre()} sigue en la Cárcel. Le quedan {jugador.getTurnosPerdidos()} turnos.";
        }
        return $"{jugador.getNombre()} está solo de visita en la Cárcel.";
    }
}

public class CasillaParqueoLibre : CasillaEspecial
{
    public CasillaParqueoLibre(string nombre, int id) : base(nombre, id) { }

    public override string aplicarCasilla(Jugador jugador, Tablero tablero)
    {
        return $"{jugador.getNombre()} descansa en el Parqueo Libre. No ocurre nada.";
    }
}

public class CasillaPolicia : CasillaEspecial
{
    public CasillaPolicia(string nombre, int id) : base(nombre, id) { }

    public override string aplicarCasilla(Jugador jugador, Tablero tablero)
    {
        EnviarJugadorACarcel(jugador, tablero);
        return $"¡La Policía detuvo a {jugador.getNombre()}! Va directo a la Cárcel por 2 turnos.";
    }

    // Lógica ÚNICA para enviar a un jugador a la cárcel. La usa esta casilla y también
    // la regla de tres dobles consecutivos (Servidor), para no duplicar la lógica de cárcel.
    public static void EnviarJugadorACarcel(Jugador jugador, Tablero tablero)
    {
        jugador.setTurnosPerdidos(2);
        // Enviar a la casilla de la Cárcel (ID 6 en el tablero de 24 casillas)
        var nodoCarcel = tablero.buscarCasillaPorID(6);
        if (nodoCarcel != null)
        {
            jugador.setNodoActual(nodoCarcel);
        }
    }
}

public class CasillaEvento : Casilla
{
    public CasillaEvento(string nombre, int id) : base(nombre, id) { }

    public override string aplicarCasilla(Jugador jugador, Tablero tablero)
    {
        var carta = tablero.MazoEventos.TomarCarta();
        int saldoActual = jugador.getSaldo();

        if (carta.ModificadorSaldo > 0)
        {
            jugador.setSaldo(saldoActual + carta.ModificadorSaldo);
        }
        else if (carta.ModificadorSaldo < 0)
        {
            int aPagar = Math.Abs(carta.ModificadorSaldo);
            if (saldoActual >= aPagar)
            {
                jugador.setSaldo(saldoActual - aPagar);
            }
            else
            {
                jugador.setSaldo(0);
                jugador.setActivo(false); // Bancarrota por evento
                return $"EVENTO_APLICADO|{carta.Titulo}|{carta.Descripcion}|Bancarrota";
            }
        }

        if (carta.CasillaDestinoId.HasValue)
        {
            var nodoDestino = tablero.buscarCasillaPorID(carta.CasillaDestinoId.Value);
            if (nodoDestino != null)
            {
                jugador.setNodoActual(nodoDestino);
            }
        }

        return $"EVENTO_APLICADO|{carta.Titulo}|{carta.Descripcion}|Saldo: ₡{jugador.getSaldo()}|{carta.ModificadorSaldo}";
    }
}