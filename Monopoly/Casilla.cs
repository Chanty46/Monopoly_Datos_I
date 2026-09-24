namespace Monopoly;

class Casilla
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

    public virtual void aplicarCasilla()
    {
        // vacio
    }
}

class Propiedad : Casilla
{
    private int precioDeCompra;
    private int alquiler;
    private string grupo;
    private Jugador duenio;
    private int casasPuestas;
    private bool tieneHotel;
    private bool estaHipotecada;

    public Propiedad(int newPrecio, int newAlquiler, string newGrupo) : this(newPrecio, newAlquiler, newGrupo, "Propiedad", 0)
    {
    }

    public Propiedad(int newPrecio, int newAlquiler, string newGrupo, string newNombre, int newID) : base(newNombre, newID)
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
    public Jugador getDuenio() { return duenio; }
    public int getCasasPuestas() { return casasPuestas; }
    public bool getTieneHotel() { return tieneHotel; }
    public bool getEstaHipotecada() { return estaHipotecada; }

    public void setPrecioDeCompra(int newPrecio) { precioDeCompra = newPrecio; }
    public void setAlquiler(int newAlquiler) { alquiler = newAlquiler; }
    public void setGrupo(string newGrupo) { grupo = newGrupo; }
    public void setDuenio(Jugador newDuenio) { duenio = newDuenio; }
    public void setCasasPuestas(int newCasas) { casasPuestas = newCasas; }
    public void setTieneHotel(bool newTieneHotel) { tieneHotel = newTieneHotel; }
    public void setEstaHipotecada(bool newValue) { estaHipotecada = newValue; }

    public bool tieneDuenio() { return duenio != null; }

    public void hipotecar()
    {
        if (duenio == null) return;
        estaHipotecada = true;
        duenio.setSaldo(duenio.getSaldo() + (alquiler / 2));
    }

    public void comprar(Jugador comprador)
    {
        if (tieneDuenio()) return;
        if (comprador.getSaldo() < precioDeCompra) return;

        duenio = comprador;
        comprador.setSaldo(comprador.getSaldo() - precioDeCompra);
        comprador.getPropiedades().agregarPropiedad(this);
    }

    public override void aplicarCasilla()
    {
        if (duenio == null) return;
        if (estaHipotecada) return;
        // La lógica real del cobro de renta se implementa en la lógica del juego.
    }
}

class CasillaEvento : Casilla
{
    public CasillaEvento(string nombre, int id) : base(nombre, id) { }
}

class CasillaEspecial : Casilla
{
    public CasillaEspecial(string nombre, int id) : base(nombre, id) { }

    public virtual void aplicarEspecial(Jugador jugador)
    {
        // Este método se redefine en cada caso especial.
    }
}

class CasillaInicial : CasillaEspecial
{
    public CasillaInicial(string nombre, int id) : base(nombre, id) { }

    public override void aplicarEspecial(Jugador jugador)
    {
        jugador.setSaldo(jugador.getSaldo() + 200);
    }
}

class CasillaCarcel : CasillaEspecial
{
    public CasillaCarcel(string nombre, int id) : base(nombre, id) { }

    public override void aplicarEspecial(Jugador jugador)
    {
        if (jugador.isEncarcelado())
        {
            jugador.setTurnosPerdidos(jugador.getTurnosPerdidos() - 1);
        }
    }
}

class CasillaParqueoLibre : CasillaEspecial
{
    public CasillaParqueoLibre(string nombre, int id) : base(nombre, id) { }

    public override void aplicarEspecial(Jugador jugador)
    {
        // No realiza ninguna acción.
    }
}

class CasillaPolicia : CasillaEspecial
{
    public CasillaPolicia(string nombre, int id) : base(nombre, id) { }

    public override void aplicarEspecial(Jugador jugador)
    {
        jugador.setTurnosPerdidos(3);
    }
}

class Eventos
{
}

