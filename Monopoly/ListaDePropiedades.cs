using System;
using System.Text;

namespace Monopoly;

public class NodoPropiedad
{
    private Propiedad propiedad;
    private NodoPropiedad? siguiente;

    public NodoPropiedad(Propiedad propiedad)
    {
        this.propiedad = propiedad;
        siguiente = null;
    }

    public Propiedad getPropiedad() => propiedad;
    public NodoPropiedad? getSiguiente() => siguiente;
    public void setSiguiente(NodoPropiedad? siguiente) => this.siguiente = siguiente;
}

// Estructura propia: Lista Simplemente Enlazada para la cartera de propiedades del jugador
public class ListaPropiedades
{
    private NodoPropiedad? head;
    private int size;

    public ListaPropiedades()
    {
        head = null;
        size = 0;
    }

    public int getSize() => size;
    public NodoPropiedad? getHead() => head;

    public void agregarPropiedad(Propiedad nuevaPropiedad)
    {
        var nuevoNodo = new NodoPropiedad(nuevaPropiedad);

        if (head == null)
        {
            head = nuevoNodo;
        }
        else
        {
            NodoPropiedad actual = head;
            while (actual.getSiguiente() != null)
            {
                actual = actual.getSiguiente()!;
            }
            actual.setSiguiente(nuevoNodo);
        }
        size++;
    }

    public bool removerPropiedad(int idCasilla)
    {
        if (head == null) return false;

        if (head.getPropiedad().getIdCasilla() == idCasilla)
        {
            head = head.getSiguiente();
            size--;
            return true;
        }

        var actual = head;
        while (actual.getSiguiente() != null)
        {
            if (actual.getSiguiente()!.getPropiedad().getIdCasilla() == idCasilla)
            {
                actual.setSiguiente(actual.getSiguiente()!.getSiguiente());
                size--;
                return true;
            }
            actual = actual.getSiguiente()!;
        }

        return false;
    }

    public Propiedad? buscarPorId(int idCasilla)
    {
        var actual = head;
        while (actual != null)
        {
            if (actual.getPropiedad().getIdCasilla() == idCasilla)
            {
                return actual.getPropiedad();
            }
            actual = actual.getSiguiente();
        }
        return null;
    }

    public int calcularValorTotal()
    {
        int total = 0;
        var actual = head;

        while (actual != null)
        {
            total += actual.getPropiedad().getPrecioDeCompra();
            actual = actual.getSiguiente();
        }

        return total;
    }

    public string obtenerListadoTexto()
    {
        if (head == null) return "Ninguna propiedad.";

        var sb = new StringBuilder();
        var actual = head;
        while (actual != null)
        {
            var p = actual.getPropiedad();
            string estado = p.getEstaHipotecada() ? "[HIPOTECADA]" : $"Alquiler: ₡{p.getAlquiler()}";
            sb.AppendLine($"  - [#{p.getIdCasilla()}] {p.getNombre()} (Grupo {p.getGrupo()}): Valor ₡{p.getPrecioDeCompra()} | {estado}");
            actual = actual.getSiguiente();
        }
        return sb.ToString().TrimEnd();
    }
}