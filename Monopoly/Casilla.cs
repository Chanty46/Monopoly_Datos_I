using System.Runtime.CompilerServices;

namespace Monopoly;
// En este se contienen todas las casillas 


class Casilla
{
    private string nombre;
    private int identificador; //Se le dara un ID para saber en que posicion del tablero esta
    // Definir la ubicacion usando la libreria de UI 
    public Casilla(string newNombre, int newID)
    {
        nombre = newNombre;
        identificador = newID;
    }
}

class Propiedad : Casilla 
{
    private int precioDeCompra;
    private int alquiler;
    private string grupo; // El grupo de propiedades sera el conjunto de colores
    private bool tieneDuenio;

    public Propiedad(int newPrecio, int newAlquiler, string newGrupo)
    {
        precioDeCompra = newPrecio;
        alquiler = newAlquiler;
        grupo = newGrupo;
        tieneDuenio = false;
    }

}


class CasillaEvento : Casilla
{

    //Idea : Esta casilla solo contendra info si es una casilla evento, pero lo que se hace es guardar un mazo de eventos por aparte que seran usados
}

class CasillaEspecial : Casilla
{
    //Idea : Esta casilla tendra un metodo que sera aplicar accion especial y luego simplemente crearemos la casilla de salida, parqueo libre, ir a la carcel y la carcel   
}

class Eventos
{
    
}
