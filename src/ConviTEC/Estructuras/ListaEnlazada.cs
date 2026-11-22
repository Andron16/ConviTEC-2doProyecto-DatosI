using System; //NotImplementedException, Predicate

namespace ConviTEC.Estructuras;

public class NodoLista<T> //nodo de la lista simplemente enlazada
{
    public T Valor { get; set; }
    public NodoLista<T> Siguiente { get; set; }
    public NodoLista(T valor) { Valor = valor; } //crea un nodo sin siguiente
}

public class ListaEnlazada<T> //lista simplemente enlazada (historial de pedidos, ventas, reservas)
{
    public int Cantidad => throw new NotImplementedException(); //número de elementos
    public bool EstaVacia => throw new NotImplementedException(); //true si no hay elementos

    public void Agregar(T valor) => throw new NotImplementedException(); //inserta al final
    public void AgregarAlInicio(T valor) => throw new NotImplementedException(); //inserta al inicio
    public T Obtener(int indice) => throw new NotImplementedException(); //lanza ArgumentOutOfRangeException si el índice no existe
    public bool EliminarEn(int indice) => throw new NotImplementedException(); //false si el índice no existe
    public T Buscar(Predicate<T> condicion) => throw new NotImplementedException(); //primer elemento que cumple; default si ninguno
    public int IndiceDe(Predicate<T> condicion) => throw new NotImplementedException(); //posición del primero que cumple; -1 si ninguno
    public T[] ComoArreglo() => throw new NotImplementedException(); //copia en orden, para devolver por la API o guardar
}