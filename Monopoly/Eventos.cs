using System;

namespace Monopoly;

public class CartaEvento
{
    public int Id { get; }
    public string Titulo { get; }
    public string Descripcion { get; }
    public int ModificadorSaldo { get; } // Positivo gana dinero, negativo paga
    public int? CasillaDestinoId { get; } // Si mueve al jugador a una casilla específica

    public CartaEvento(int id, string titulo, string descripcion, int modificadorSaldo, int? casillaDestino = null)
    {
        Id = id;
        Titulo = titulo;
        Descripcion = descripcion;
        ModificadorSaldo = modificadorSaldo;
        CasillaDestinoId = casillaDestino;
    }

    public override string ToString()
    {
        string efecto = ModificadorSaldo >= 0 ? $"+₡{ModificadorSaldo}" : $"-₡{Math.Abs(ModificadorSaldo)}";
        return $"[EVENTO] {Titulo}: {Descripcion} ({efecto})";
    }
}

public class NodoEvento
{
    public CartaEvento Carta { get; }
    public NodoEvento? Siguiente { get; set; }

    public NodoEvento(CartaEvento carta)
    {
        Carta = carta;
        Siguiente = null;
    }
}

// Estructura propia tipo Cola / Mazo circular para los eventos del juego
public class MazoEventos
{
    private NodoEvento? head;
    private NodoEvento? tail;
    private int cantidad;

    public MazoEventos()
    {
        head = null;
        tail = null;
        cantidad = 0;
        CargarEventosPorDefecto();
    }

    public void AgregarCarta(CartaEvento carta)
    {
        var nuevo = new NodoEvento(carta);
        if (head == null)
        {
            head = nuevo;
            tail = nuevo;
            tail.Siguiente = head; // Enlace circular para rotación infinita
        }
        else
        {
            nuevo.Siguiente = head;
            tail!.Siguiente = nuevo;
            tail = nuevo;
        }
        cantidad++;
    }

    // Extrae la carta superior y la coloca al fondo (rotación del mazo)
    public CartaEvento TomarCarta()
    {
        if (head == null)
        {
            return new CartaEvento(0, "Suerte", "Día tranquilo, no ocurre nada.", 0);
        }

        var cartaTomada = head.Carta;
        // Avanzar el mazo circular
        tail = head;
        head = head.Siguiente;
        return cartaTomada;
    }

    private void CargarEventosPorDefecto()
    {
        AgregarCarta(new CartaEvento(1, "Lotería Nacional", "¡Ganaste la lotería semanal! Cobra ₡100.", 100));
        AgregarCarta(new CartaEvento(2, "Impuesto Municipal", "Debes pagar impuestos atrasados. Paga ₡50.", -50));
        AgregarCarta(new CartaEvento(3, "Error Bancario", "El banco cometió un error a tu favor. Cobra ₡150.", 150));
        AgregarCarta(new CartaEvento(4, "Multa de Tránsito", "Exceso de velocidad camino a la propiedad. Paga ₡50.", -50));
        AgregarCarta(new CartaEvento(5, "Reparaciones", "Mantenimiento general en tus propiedades. Paga ₡75.", -75));
        AgregarCarta(new CartaEvento(6, "Reintegro de Seguro", "La aseguradora te devuelve primas no utilizadas. Cobra ₡50.", 50));
        AgregarCarta(new CartaEvento(7, "Gastos Médicos", "Visita imprevista al hospital. Paga ₡100.", -100));
        AgregarCarta(new CartaEvento(8, "Premio al Emprendimiento", "Premio al negocio del año. Cobra ₡200.", 200));
    }
}
