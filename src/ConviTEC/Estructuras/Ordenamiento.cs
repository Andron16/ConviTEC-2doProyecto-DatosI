using System; //Comparison

namespace ConviTEC.Estructuras;

public static class Ordenamiento //algoritmos de ordenamiento propios (no se permite Sort() ni OrderBy())
{
    public static void Ordenar<T>(T[] datos, Comparison<T> comparar) //merge sort, ordena en el mismo arreglo
    {
        if (datos == null || datos.Length < 2) return; //vacío o de un elemento: ya está ordenado
        T[] auxiliar = new T[datos.Length]; //un solo arreglo de apoyo para todas las mezclas
        MergeSort(datos, auxiliar, 0, datos.Length - 1, comparar);
    }

    private static void MergeSort<T>(T[] datos, T[] auxiliar, int inicio, int fin, Comparison<T> comparar) //divide en mitades hasta tener un elemento
    {
        if (inicio >= fin) return;
        int medio = (inicio + fin) / 2;
        MergeSort(datos, auxiliar, inicio, medio, comparar);
        MergeSort(datos, auxiliar, medio + 1, fin, comparar);
        Mezclar(datos, auxiliar, inicio, medio, fin, comparar);
    }

    private static void Mezclar<T>(T[] datos, T[] auxiliar, int inicio, int medio, int fin, Comparison<T> comparar) //une dos mitades ya ordenadas
    {
        for (int k = inicio; k <= fin; k++) auxiliar[k] = datos[k]; //copia el tramo

        int i = inicio, j = medio + 1;
        for (int k = inicio; k <= fin; k++)
        {
            if (i > medio) datos[k] = auxiliar[j++]; //se acabó la mitad izquierda
            else if (j > fin) datos[k] = auxiliar[i++]; //se acabó la mitad derecha
            else if (comparar(auxiliar[i], auxiliar[j]) <= 0) datos[k] = auxiliar[i++]; //<= mantiene el orden de los iguales (estable)
            else datos[k] = auxiliar[j++];
        }
    }
}