using System; //ArgumentException

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

    public bool EsAlcanzable(string destino) //false si no existe o no hay camino
    {
        int indice = Grafo.IndiceDe(destino);
        return indice != -1 && Distancias[indice] != GrafoMatriz.SinArista;
    }

    public int TiempoHasta(string destino) //minutos mínimos; -1 si es inalcanzable
    {
        if (!EsAlcanzable(destino)) return -1;
        return Distancias[Grafo.IndiceDe(destino)];
    }

    public string[] CaminoHasta(string destino) //nodos desde el origen hasta el destino; arreglo vacío si es inalcanzable
    {
        if (!EsAlcanzable(destino)) return new string[0];
        int fin = Grafo.IndiceDe(destino);

        int largo = 0; //primero se cuentan los nodos del camino siguiendo los previos
        for (int v = fin; v != -1; v = Previos[v]) largo++;

        string[] camino = new string[largo]; //se llena de atrás hacia adelante: así queda ya invertido (origen primero)
        int k = largo - 1;
        for (int v = fin; v != -1; v = Previos[v]) camino[k--] = Grafo.NombreDe(v);
        return camino;
    }
}

public static class Dijkstra //algoritmo de caminos mínimos implementado por el grupo
{
    public static ResultadoDijkstra Calcular(GrafoMatriz grafo, string origen) //ArgumentException si el origen no existe
    {
        if (grafo == null) throw new ArgumentException("No hay grafo cargado.");
        int inicio = grafo.IndiceDe(origen);
        if (inicio == -1) throw new ArgumentException($"El nodo {origen} no existe en el grafo.");

        int n = grafo.CantidadNodos;
        int[] distancias = new int[n]; //mejor tiempo conocido hasta cada nodo
        int[] previos = new int[n]; //de dónde se llegó a cada nodo
        bool[] visitados = new bool[n]; //nodos cuya distancia ya es definitiva
        for (int i = 0; i < n; i++) { distancias[i] = GrafoMatriz.SinArista; previos[i] = -1; }
        distancias[inicio] = 0;

        for (int vuelta = 0; vuelta < n; vuelta++)
        {
            int u = -1; //el no visitado con menor distancia (búsqueda lineal en el arreglo)
            for (int i = 0; i < n; i++)
                if (!visitados[i] && distancias[i] != GrafoMatriz.SinArista && (u == -1 || distancias[i] < distancias[u])) u = i;
            if (u == -1) break; //lo que queda es inalcanzable desde el origen
            visitados[u] = true;

            for (int v = 0; v < n; v++) //relajación: ¿llegar a v pasando por u es más rápido?
            {
                int peso = grafo.Peso(u, v);
                if (visitados[v] || peso == GrafoMatriz.SinArista) continue; //se revisa SinArista ANTES de sumar (int.MaxValue + x se desborda)
                int nueva = distancias[u] + peso;
                if (nueva < distancias[v]) { distancias[v] = nueva; previos[v] = u; }
            }
        }
        return new ResultadoDijkstra(grafo, inicio, distancias, previos);
    }
}