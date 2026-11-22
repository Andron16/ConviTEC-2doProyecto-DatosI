using System; //NotImplementedException

namespace ConviTEC.Grafo;

public class ResultadoDijkstra //distancias y caminos mínimos desde un origen
{
    public GrafoMatriz Grafo { get; }
    public int Origen { get; } //índice del nodo de origen
    public int[] Distancias { get; } //minutos mínimos a cada nodo; GrafoMatriz.SinArista si es inalcanzable
    public int[] Previos { get; } //nodo anterior en el camino mínimo; -1 si no tiene

    public ResultadoDijkstra(GrafoMatriz grafo, int origen, int[] distancias, int[] previos) //guarda el resultado del cálculo
    {
        Grafo = grafo; Origen = origen; Distancias = distancias; Previos = previos;
    }

    public bool EsAlcanzable(string destino) => throw new NotImplementedException(); //false si no existe o no hay camino
    public int TiempoHasta(string destino) => throw new NotImplementedException(); //minutos mínimos; -1 si es inalcanzable
    public string[] CaminoHasta(string destino) => throw new NotImplementedException(); //nodos desde el origen hasta el destino; arreglo vacío si es inalcanzable
}

public static class Dijkstra //algoritmo de caminos mínimos implementado por el grupo
{
    public static ResultadoDijkstra Calcular(GrafoMatriz grafo, string origen) => throw new NotImplementedException(); //ArgumentException si el origen no existe
}