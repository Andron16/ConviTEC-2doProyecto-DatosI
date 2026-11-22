using System; //NotImplementedException

namespace ConviTEC.Estructuras;

public class Cola<T> //cola FIFO (pedidos pendientes de entrega)
{
    public int Cantidad => throw new NotImplementedException(); //número de elementos
    public bool EstaVacia => throw new NotImplementedException(); //true si no hay elementos

    public void Encolar(T valor) => throw new NotImplementedException(); //agrega al final
    public T Desencolar() => throw new NotImplementedException(); //quita el del frente; InvalidOperationException si está vacía
    public T VerFrente() => throw new NotImplementedException(); //consulta el del frente sin quitarlo; InvalidOperationException si está vacía
    public T[] ComoArreglo() => throw new NotImplementedException(); //copia del frente al final
}