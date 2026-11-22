using System; //NotImplementedException, Comparison

namespace ConviTEC.Estructuras;

public static class Ordenamiento //algoritmos de ordenamiento propios (no se permite Sort() ni OrderBy())
{
    public static void Ordenar<T>(T[] datos, Comparison<T> comparar) => throw new NotImplementedException(); //merge sort, ordena en el mismo arreglo
}