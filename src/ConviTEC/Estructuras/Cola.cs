using System; //InvalidOperationException

namespace ConviTEC.Estructuras;

public class Cola<T> //cola FIFO (pedidos pendientes de entrega)
{
    private NodoLista<T> frente; //el que sale primero
    private NodoLista<T> final; //el último en llegar
    private int cantidad; //número de elementos

    public int Cantidad => cantidad; //número de elementos
    public bool EstaVacia => cantidad == 0; //true si no hay elementos

    public void Encolar(T valor) //agrega al final
    {
        NodoLista<T> nuevo = new NodoLista<T>(valor);
        if (final == null) { frente = nuevo; final = nuevo; } //cola vacía
        else { final.Siguiente = nuevo; final = nuevo; }
        cantidad++;
    }

    public T Desencolar() //quita el del frente; InvalidOperationException si está vacía
    {
        if (frente == null) throw new InvalidOperationException("La cola está vacía.");
        T valor = frente.Valor;
        frente = frente.Siguiente;
        if (frente == null) final = null; //se vació: el final también se reinicia
        cantidad--;
        return valor;
    }

    public T VerFrente() //consulta el del frente sin quitarlo; InvalidOperationException si está vacía
    {
        if (frente == null) throw new InvalidOperationException("La cola está vacía.");
        return frente.Valor;
    }

    public T[] ComoArreglo() //copia del frente al final
    {
        T[] arreglo = new T[cantidad];
        int i = 0;
        for (NodoLista<T> actual = frente; actual != null; actual = actual.Siguiente)
            arreglo[i++] = actual.Valor;
        return arreglo;
    }
}