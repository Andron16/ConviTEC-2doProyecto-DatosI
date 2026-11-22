using System; //NotImplementedException
using ConviTEC.Modelos; //Ruta

namespace ConviTEC.Grafo;

public class GrafoMatriz //grafo no dirigido y ponderado, implementado con matriz de adyacencia
{
    public const int SinArista = int.MaxValue; //valor en la matriz cuando no hay ruta entre dos nodos

    public GrafoMatriz(int capacidad) { } //crea la matriz para hasta 'capacidad' nodos (lo implementa B)

    public int CantidadNodos => throw new NotImplementedException(); //nodos agregados hasta ahora

    public int AgregarNodo(string nombre) => throw new NotImplementedException(); //devuelve su índice; si ya existe, devuelve el existente
    public bool AgregarArista(string origen, string destino, int tiempo) => throw new NotImplementedException(); //ruta en ambos sentidos; false si un nodo no existe o tiempo <= 0
    public int IndiceDe(string nombre) => throw new NotImplementedException(); //-1 si el nodo no existe
    public string NombreDe(int indice) => throw new NotImplementedException(); //nombre del nodo en esa posición
    public int Peso(int origen, int destino) => throw new NotImplementedException(); //minutos entre dos índices; SinArista si no hay ruta
    public string[] Nombres() => throw new NotImplementedException(); //todos los nodos, en orden de índice
    public Ruta[] Aristas() => throw new NotImplementedException(); //cada ruta una sola vez (para GET /api/grafo)
}