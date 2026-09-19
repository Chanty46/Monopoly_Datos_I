using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Monopoly;
// En este se contienen todas las casillas 


class Casilla
{
    private string nombre;
    private int idCasilla; //Se le dara un ID para saber en que posicion del tablero esta
    // Definir la ubicacion usando la libreria de UI 
    public Casilla(string newNombre, int newID)
    {
        nombre = newNombre;
        idCasilla = newID;
    }
   
    // ================= GETTERS Y SETTERS =================
    public string getNombre() { return nombre; }
    public int getIdCasilla() { return idCasilla; }

    public void setNombre(string newNombre) { nombre = newNombre; }
    public void setIdCasilla(int newID) { idCasilla = newID; }

    public virtual void aplicarCasilla(Jugador jugador)
    {
        // vacio
    }

}


class Propiedad : Casilla 
{
    // Dinero y Grupo
    private int precioDeCompra;
    private int alquiler;
    private string grupo; // El grupo de propiedades sera el conjunto de colores
    
    // Duenio
    private Jugador duenio;

    // Estado de la Propiedad
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

    // ================= GETTERS Y SETTERS =================
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

    // Metodos 

    public bool tieneDuenio() { return duenio != null; }

    public void hipotecar() //esto se podra hacer atraves de un panel de propiedades del jugador
    {
        if(casasPuestas == 0 && !tieneHotel)
        {
        setEstaHipotecada(true); //Al hipotecarse, esta ya no puede cobrar renta
        duenio.setSaldo(duenio.getSaldo() + (precioDeCompra/2)); //Le da la mitad del valor de la propiedad al duenio
        } else {
            return; //No se puede hipotecar una propiedad si no se tienen casas
        }
    }

    public void desHipotecar()
    {
        if(duenio.getSaldo() > (precioDeCompra/2 + precioDeCompra/10)) //Todas las propiedades terminaran con un 0 al final para que no nos de error
        {
            duenio.setSaldo(duenio.getSaldo() - (precioDeCompra/2 + precioDeCompra/10));
            setEstaHipotecada(false);
        } 
    }

    public void comprar(Jugador comprador)
    {
        // Caso 1, tiene duenio!
        if (tieneDuenio())
        {
            return; //no se puede comprar
        }
        // Caso 2, no le alcanza la propiedad
        if(comprador.getSaldo() < this.getPrecioDeCompra())
        {
            return;
        }

        //Caso 3, la compra!
        this.setDuenio(comprador);
        comprador.setSaldo(comprador.getSaldo() - this.getPrecioDeCompra());

        //Luego agregarla a su lista de propiedades
        comprador.getPropiedades().agregarPropiedad(this); //Agrega esta misma propiedad a la lista de propiedades   
    }

    // Aplicar Casilla se aplica AL final del turno
    public void cobrarAlquiler(Jugador jugador)
    {
        if(duenio == jugador || estaHipotecada)
        {
            return; //No se le cobra renta al propio jugador jaja o  si esta hipotecada no cobra renta
        } else {
            if(jugador.getSaldo() > alquiler)
            {
            jugador.setSaldo(jugador.getSaldo() - alquiler); //asegurarnos de que le alcanze el alquiler al jugador
            duenio.setSaldo(duenio.getSaldo() + alquiler); //es como un traspaso de dinero, al jugador que cae sobre la propiedad pierde el dinero y al otro se le aumenta
            } else {
                //Aqui tocaria o hacer un trueque entre jugadores o bien declarar bancarrota y todo lo que tenga el jugadoor a cobrar 
                // Se le pasa al jugador duenio de la propiedad
            }

        }
    }
    public override void aplicarCasilla(Jugador jugador) //Esto de aqui seria cobrar la renta, que es lo que mas se aplica
    {
        if (!tieneDuenio()) //Si no tiene duenio
        {
            //Se debe preguntar si se quiere comprar la casilla
            //comprar(jugador); //Esto es basico pero se debe desarrollar una UI que permita o comprar o hacer otro asunto, hay que solucionar esto
        } else {
            cobrarAlquiler(jugador);
        }
        
        //Si no tiene duenio, verificar si comprar, pero esto hay que cambiarlo con la UI por obvias razones
        // si tiene dueino se cobra la renta, 
            // al cobrar la renta verificar que no este hipotecada, si esta hipotecada no cobra renta y esta mas chileadito. 
    }
}


class CasillaEvento : Casilla
{
    public CasillaEvento(string nombre, int id) : base(nombre, id) { }
 //Logica 
 // Tenemos que crear una clase que contenga eventos y luego hacemos polimorfismo donde movemos a los jugadores, cambiamos sus variables, etc,
 // Por el momento se quedara asi 
}

class CasillaEspecial : Casilla
{
    public CasillaEspecial(string nombre, int id) : base(nombre, id) { }

    public virtual void aplicarCasilla(Jugador jugador)
    {
        return; // Este de aqui cambiara
    }
}

class CasillaInicial : CasillaEspecial
{
    public CasillaInicial(string nombre, int id) : base(nombre, id) { }

    public override void aplicarCasilla(Jugador jugador) //Jablar de aplicar casilla en vez de aplicarCasilla
    {
        jugador.setSaldo(jugador.getSaldo() + 200);
    }
}

class CasillaCarcel : CasillaEspecial
{
      /**
     * IDEA 
     * Ok esta casilla funciona de dos maneras, como visita (pasa normal)
     * Encarcelado, si el jugador esta encarcelado (turnosPerdidos > 0), simplemente le baja el turno, para esto mismo hay que revisar su posicion antes de empezar a moverse. 
     * Preguntar si ID de la casilla es tal, entonces apliquemos la funcion de la casilla.  
     */    
    public CasillaCarcel(string nombre, int id) : base(nombre, id) { }

    public override void aplicarCasilla(Jugador jugador)
    {
        if (jugador.isEncarcelado())
        {
            jugador.setTurnosPerdidos(jugador.getTurnosPerdidos() - 1);
        }
    }
}

class CasillaParqueoLibre : CasillaEspecial {
    public CasillaParqueoLibre(string nombre, int id) : base(nombre, id) { }

    public override void aplicarCasilla(Jugador jugador)
    {
        // No realiza ninguna acción
    }
}

class CasillaPolicia : CasillaEspecial
{
    public CasillaPolicia(string nombre, int id) : base(nombre, id) { }

    public override void aplicarCasilla(Jugador jugador)
    {
        jugador.setTurnosPerdidos(3);
        // La lógica del movimiento a la Cárcel la ejecuta el Tablero o el Banco 
        // Hay que decidir cual sera el ID de la carcel 
    }
}

class Eventos
{
    
}