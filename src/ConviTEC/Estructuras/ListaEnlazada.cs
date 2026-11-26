using System; //ArgumentOutOfRangeException, Predicate

namespace ConviTEC.Estructuras;

public class NodoLista<T> //nodo de la lista simplemente enlazada
{
    public T Valor { get; set; }
    public NodoLista<T> Siguiente { get; set; }
    public NodoLista(T valor) { Valor = valor; } //crea un nodo sin siguiente
}

public class ListaEnlazada<T> //lista simplemente enlazada (historial de pedidos, ventas, reservas)
{
    private NodoLista<T> cabeza; //primer nodo
    private NodoLista<T> cola; //último nodo: permite agregar al final sin recorrer la lista
    private int cantidad; //número de nodos

    public int Cantidad => cantidad; //número de elementos
    public bool EstaVacia => cantidad == 0; //true si no hay elementos

    public void Agregar(T valor) //inserta al final
    {
        NodoLista<T> nuevo = new NodoLista<T>(valor);
        if (cabeza == null) { cabeza = nuevo; cola = nuevo; } //lista vacía: el nodo es inicio y final
        else { cola.Siguiente = nuevo; cola = nuevo; }
        cantidad++;
    }

    public void AgregarAlInicio(T valor) //inserta al inicio
    {
        NodoLista<T> nuevo = new NodoLista<T>(valor);
        nuevo.Siguiente = cabeza;
        cabeza = nuevo;
        if (cola == null) cola = nuevo; //era la lista vacía
        cantidad++;
    }

    public T Obtener(int indice) //lanza ArgumentOutOfRangeException si el índice no existe
    {
        if (indice < 0 || indice >= cantidad) throw new ArgumentOutOfRangeException(nameof(indice), "El índice no existe en la lista.");
        return NodoEn(indice).Valor;
    }

    public bool EliminarEn(int indice) //false si el índice no existe
    {
        if (indice < 0 || indice >= cantidad) return false;
        if (indice == 0) //quitar la cabeza
        {
            cabeza = cabeza.Siguiente;
            if (cabeza == null) cola = null; //quedó vacía
        }
        else
        {
            NodoLista<T> anterior = NodoEn(indice - 1);
            anterior.Siguiente = anterior.Siguiente.Siguiente; //se salta el nodo eliminado
            if (anterior.Siguiente == null) cola = anterior; //se borró el último: el anterior pasa a ser la cola
        }
        cantidad--;
        return true;
    }

    public T Buscar(Predicate<T> condicion) //primer elemento que cumple; default si ninguno
    {
        for (NodoLista<T> actual = cabeza; actual != null; actual = actual.Siguiente)
            if (condicion(actual.Valor)) return actual.Valor;
        return default;
    }

    public int IndiceDe(Predicate<T> condicion) //posición del primero que cumple; -1 si ninguno
    {
        int i = 0;
        for (NodoLista<T> actual = cabeza; actual != null; actual = actual.Siguiente, i++)
            if (condicion(actual.Valor)) return i;
        return -1;
    }

    public T[] ComoArreglo() //copia en orden, para devolver por la API o guardar
    {
        T[] arreglo = new T[cantidad];
        int i = 0;
        for (NodoLista<T> actual = cabeza; actual != null; actual = actual.Siguiente)
            arreglo[i++] = actual.Valor;
        return arreglo;
    }

    private NodoLista<T> NodoEn(int indice) //recorre desde la cabeza hasta la posición (el índice ya viene validado)
    {
        NodoLista<T> actual = cabeza;
        for (int i = 0; i < indice; i++) actual = actual.Siguiente;
        return actual;
    }
}