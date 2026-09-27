using System.ComponentModel;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace Monopoly;
// En este se contienen todas las casillas 

// CASILLA BASE 
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

    public virtual bool aplicarCasilla(Jugador jugador)
    {
        return true;
    }

}

// PROPIEDAD
class Propiedad : Casilla 
{
    // Dinero y Grupo
    private int precioDeCompra;
    private int alquiler;
    private int alquilerBase;
    private string grupo; // El grupo de propiedades sera el conjunto de colores
    private int grupoSize;
    
    // Duenio
    private Jugador duenio;

    // Estado de la Propiedad
    private int casasPuestas;
    private bool tieneHotel;
    private bool estaHipotecada;

    public Propiedad(string newNombre, int newID, int newPrecio, int newAlquiler, string newGrupo, int newGrupoSize) 
        : base(newNombre, newID)
    {
        precioDeCompra = newPrecio;
        alquiler = newAlquiler;
        alquilerBase = alquiler;
        grupo = newGrupo;
        grupoSize = newGrupoSize;
        casasPuestas = 0;
        tieneHotel = false;
        estaHipotecada = false;
        duenio = null; //Aun no tiene duenio entonces no podemos solventar esto mismo
    }

    // ================= GETTERS Y SETTERS =================
    public int getPrecioDeCompra() { return precioDeCompra; }
    public int getAlquiler() { return alquiler; }
    public string getGrupo() { return grupo; }
    public int getGrupoSize() {return grupoSize;}
    public Jugador getDuenio() { return duenio; }
    public int getCasasPuestas() { return casasPuestas; }
    public bool getTieneHotel() { return tieneHotel; }
    public bool getEstaHipotecada() { return estaHipotecada; }
    public int getAlquilerBase() {return alquilerBase; }

    public void setPrecioDeCompra(int newPrecio) { precioDeCompra = newPrecio; }
    public void setAlquiler(int newAlquiler) { alquiler = newAlquiler; }
    public void setDuenio(Jugador newDuenio) { duenio = newDuenio; } //Esta nos sirve por si hay jugadores que quieren intercambiar
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

    public void comprar(Jugador comprador) // REQUIERE LA TARJETA ELECTRONICA
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
    public virtual bool cobrarAlquiler(Jugador jugador) //Bool nos permitira saber si se pudo cobrar o no 
    {
        if(duenio == jugador || estaHipotecada)
        {
            return true; //No se le cobra renta al propio jugador jaja o  si esta hipotecada no cobra renta
        } else {
            if(jugador.getSaldo() > alquiler)
            {
            jugador.setSaldo(jugador.getSaldo() - alquiler); //asegurarnos de que le alcanze el alquiler al jugador
            duenio.setSaldo(duenio.getSaldo() + alquiler); //es como un traspaso de dinero, al jugador que cae sobre la propiedad pierde el dinero y al otro se le aumenta
            return true;
            } else {
                return false; //La idea seria que Banco lo maneje
            }

        }
    }
    public override bool aplicarCasilla(Jugador jugador) //Esto de aqui seria cobrar la renta, que es lo que mas se aplica
    {
        if (!tieneDuenio()) //Si no tiene duenio
        {
            //Se debe preguntar si se quiere comprar la casilla
            //comprar(jugador); //Esto es basico pero se debe desarrollar una UI que permita o comprar o hacer otro asunto, hay que solucionar esto
            return true;
        } else {
            bool res = cobrarAlquiler(jugador);
            return res;
        }
        
        //Si no tiene duenio, verificar si comprar, pero esto hay que cambiarlo con la UI por obvias razones
        // si tiene dueino se cobra la renta, 
            // al cobrar la renta verificar que no este hipotecada, si esta hipotecada no cobra renta y esta mas chileadito. 
    }

    //Manejar casas

    //Verificar que le alcanze la casa?, esto lo puede hacer el banco con ManejarAgregarCasa
    public virtual bool agregarCasa(){
        if (estaHipotecada || duenio.getPropiedades().puedeMejorar(this) == false || getTieneHotel())
        {
            return false;

        } else {
            if(getCasasPuestas() < 4)
            {
                setCasasPuestas(getCasasPuestas() + 1);
                setAlquiler(calcularAlquilerActual());
                return true;
            } else {
                setTieneHotel(true);
                setAlquiler(calcularAlquilerActual());
                return true;
            }
        }

    }

    public virtual bool removerCasa()
{
    if (estaHipotecada || !duenio.getPropiedades().puedeQuitarCasa(this))
    {
        return false;
    }  if (tieneHotel) {
        setTieneHotel(false);
        setCasasPuestas(4); //Evitar errores
        calcularAlquilerActual();
        return true;
    }

    if (casasPuestas > 0)
    {
        setCasasPuestas(casasPuestas - 1);
        setAlquiler(calcularAlquilerActual());
        return true;
    }

    return false;
}
public virtual int calcularAlquilerActual()
{
    if (casasPuestas == 0 && !tieneHotel)
    {
        bool grupoCompleto = duenio.getPropiedades().tieneGrupo(this);
        int multiplicadorGrupo = grupoCompleto ? 2 : 1; // ? es un operador ternario entonces decimos 2 si true, 1 si false pero mas corto
        return getAlquilerBase() * multiplicadorGrupo;
    }

    int multiplicador = getTieneHotel() switch
    {
        true => 20,
        false => getCasasPuestas() switch
        {
            4 => 15,
            3 => 10,
            2 => 6,
            1 => 3,
            _ => 1  //Cualquier otro valor
        }
    };

    return getAlquilerBase() * multiplicador;
}
}

class Ferrocarril : Propiedad
{
    public Ferrocarril(string newNombre, int newID, int newPrecio) 
        : base(newNombre, newID, newPrecio, 0, "Ferrocarril", 4)
    {
        // alquiler = 0 porque no se usa el campo fijo, se calcula dinámicamente
    }

    public override bool aplicarCasilla(Jugador jugador)
    {
        if (!tieneDuenio())
        {
            //Se debe preguntar si se quiere comprar la casilla (igual que en Propiedad)
            //comprar(jugador);
            return true;
        }
        else
        {
            bool res = cobrarAlquiler(jugador);
            return res;
        }
    }

    public override bool cobrarAlquiler(Jugador jugador)
    {
        if (getDuenio() == jugador || getEstaHipotecada())
        {
            return true; //No se cobra al propio dueño, ni si está hipotecado
        }

        int cantidadFerrocarriles = getDuenio().getPropiedades().contarFerrocarriles();

        int montoACobrar = cantidadFerrocarriles switch //Esto de aqui es con Cases, contamos cuantos ferrocarriles se tienen para luego comprarlos
        {
            1 => 25,
            2 => 50,
            3 => 100,
            4 => 200,
            _ => 0
        };

        if (jugador.getSaldo() > montoACobrar)
        {
            jugador.setSaldo(jugador.getSaldo() - montoACobrar);
            getDuenio().setSaldo(getDuenio().getSaldo() + montoACobrar);
            return true;
        }
        return false; // NO se logró cobrar
        //Igual que en Propiedad: falta resolver el caso de saldo insuficiente (bancarrota)
    }
    public override bool agregarCasa(){return false;}
    public override bool removerCasa(){return false;}
    public override int calcularAlquilerActual(){return 0;}
}

class CasillaServicio : Propiedad
{
    public CasillaServicio(string newNombre, int newID, int newPrecio) 
        : base(newNombre, newID, newPrecio, 0, "Servicio", 2)
    {
    }

    public override bool aplicarCasilla(Jugador jugador)
    {
        if (!tieneDuenio())
        {
            //comprar(jugador); // pendiente, igual que las demás
            return true;
        }
        else
        {
           bool res = cobrarAlquiler(jugador);
           return res;
        }
    }

    public override bool cobrarAlquiler(Jugador jugador)
    {
        if (getDuenio() == jugador || getEstaHipotecada())
        {
            return true;
        }

        int valorPatrimonio = jugador.getPropiedades().calcularValorTotal();
        int montoACobrar = Math.Max(50, valorPatrimonio / 10); // Math Max devuelve el mayor entre 50 o el 10/ del patrimonio

        if (jugador.getSaldo() > montoACobrar)
        {
            jugador.setSaldo(jugador.getSaldo() - montoACobrar);
            getDuenio().setSaldo(getDuenio().getSaldo() + montoACobrar);
            return true;
        }
        return false;
    }
    public override bool agregarCasa(){return false;}
    public override bool removerCasa(){return false;}
    public override int calcularAlquilerActual(){return 0;}
}

// EVENTOS


class CasillaEspecial : Casilla //Realmente esta casilla es solo para formalidades pero no sirve de mucho
{
    public CasillaEspecial(string nombre, int id) : base(nombre, id) { }
}

class CasillaEvento : CasillaEspecial
{
    private ListaCircularCartas mazoEventos;

    public CasillaEvento(string nombre, int id, ListaCircularCartas mazoEventos) : base(nombre, id)
    {
        this.mazoEventos = mazoEventos;
    }

    // Getter y Setter para el mazo de eventos
    public ListaCircularCartas getMazoEventos() { return mazoEventos; }
    public void setMazoEventos(ListaCircularCartas nuevoMazo) { mazoEventos = nuevoMazo; }

    public override bool aplicarCasilla(Jugador jugador)
    {
        if (mazoEventos != null) //validación por si el mazo está vacío
        {
            CartaEvento carta = mazoEventos.tomarCarta();
            if (carta != null) //validacion por si la referencia carta esta vacia
            {
                //Aplica el efecto de la crta sobre el jugador (metodo en eventos.cs)
                carta.aplicarEfecto(jugador);
            }
        }
        return true;
    }
}

class CasillaInicial : CasillaEspecial
{
    public CasillaInicial(string nombre, int id) : base(nombre, id) { }

    public override bool aplicarCasilla(Jugador jugador) //Jablar de aplicar casilla en vez de aplicarCasilla
    {
        jugador.setSaldo(jugador.getSaldo() + 200);
        return true;
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

    public override bool aplicarCasilla(Jugador jugador)
    {
        if (jugador.getEstaEncarcelado())
        {
            //Aplicar carta salir de carcel!! 
            jugador.setTurnosPerdidos(jugador.getTurnosPerdidos() - 1);
            return true;
        } else {
            return true; //No hace nada!
        }
    }
}

class CasillaParqueoLibre : CasillaEspecial {
    public CasillaParqueoLibre(string nombre, int id) : base(nombre, id) { }

    // No es necesario hacer override al metodo original, pues esta casilla no hace nada
}

class CasillaPolicia : CasillaEspecial
{
    public CasillaPolicia(string nombre, int id) : base(nombre, id) { }

    public override bool aplicarCasilla(Jugador jugador)
    {
        jugador.setTurnosPerdidos(3);
        jugador.setEstaEncarcelado(true);
        return true;
        //Ahora, del resto se encargará la clase tablero
    }
}
class CasillaBanco : CasillaEspecial
 
    {
        public override bool aplicarCasilla(Jugador jugador)
        {
            jugador.setSaldo(jugador.getSaldo() - 200); //Pagar 200 en esta casilla  
            return true; //AL banco si le puede deber plata!
        }
        public CasillaBanco(string nombre, int id) : base(nombre, id)
        {
        }

    }
 
