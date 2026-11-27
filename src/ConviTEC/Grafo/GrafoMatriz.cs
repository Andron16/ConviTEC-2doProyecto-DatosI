using System; //Math
using ConviTEC.Modelos; //Ruta

namespace ConviTEC.Grafo;

public class GrafoMatriz //grafo no dirigido y ponderado, implementado con matriz de adyacencia
{
    public const int SinArista = int.MaxValue; //valor en la matriz cuando no hay ruta entre dos nodos

    private int[,] matriz; //matriz[i, j] = minutos entre el nodo i y el j; SinArista si no hay ruta
    private string[,] idsRutas; //id de la ruta guardada en cada celda ("R01"...)
    private string[] nombres; //nombres[i] = nombre del nodo con índice i
    private int cantidad; //nodos agregados hasta ahora
    private int cantidadAristas; //rutas distintas (cada una cuenta una vez aunque esté en dos celdas)

    public GrafoMatriz(int capacidad) //crea la matriz para hasta 'capacidad' nodos
    {
        if (capacidad < 0) capacidad = 0; //nunca lanza excepción: Program.cs usa new GrafoMatriz(0) como respaldo
        CrearMatriz(capacidad);
    }

    public int CantidadNodos => cantidad; //nodos agregados hasta ahora

    public int AgregarNodo(string nombre) //devuelve su índice; si ya existe, devuelve el existente
    {
        if (nombre == null) throw new ArgumentException("El nombre del nodo no puede ser nulo.");
        int existente = IndiceDe(nombre);
        if (existente != -1) return existente; //no se duplican nodos

        if (cantidad == nombres.Length) Ampliar(); //matriz llena: se agranda al doble
        nombres[cantidad] = nombre;
        cantidad++;
        return cantidad - 1;
    }

    public bool AgregarArista(string origen, string destino, int tiempo) //ruta en ambos sentidos; false si un nodo no existe o tiempo <= 0
    {
        int i = IndiceDe(origen);
        int j = IndiceDe(destino);
        if (i == -1 || j == -1 || i == j || tiempo <= 0) return false; //i == j: no se permiten lazos (la diagonal queda en 0)

        if (matriz[i, j] == SinArista) //ruta nueva: recibe el siguiente id
        {
            cantidadAristas++;
            string id = "R" + cantidadAristas.ToString("D2");
            idsRutas[i, j] = id;
            idsRutas[j, i] = id;
        }
        matriz[i, j] = tiempo; //no dirigido: se guarda en las dos celdas
        matriz[j, i] = tiempo;
        return true;
    }

    public int IndiceDe(string nombre) //-1 si el nodo no existe (búsqueda lineal, comparación exacta con tildes)
    {
        if (nombre == null) return -1;
        for (int i = 0; i < cantidad; i++)
            if (nombres[i] == nombre) return i;
        return -1;
    }

    public string NombreDe(int indice) //nombre del nodo en esa posición; null si el índice no existe
    {
        if (indice < 0 || indice >= cantidad) return null;
        return nombres[indice];
    }

    public int Peso(int origen, int destino) //minutos entre dos índices; SinArista si no hay ruta
    {
        if (origen < 0 || origen >= cantidad || destino < 0 || destino >= cantidad) return SinArista; //índice inválido = sin ruta
        return matriz[origen, destino];
    }

    public string[] Nombres() //todos los nodos, en orden de índice
    {
        string[] copia = new string[cantidad]; //copia: nadie de afuera modifica el arreglo interno
        for (int i = 0; i < cantidad; i++) copia[i] = nombres[i];
        return copia;
    }

    public Ruta[] Aristas() //cada ruta una sola vez (para GET /api/grafo)
    {
        Ruta[] rutas = new Ruta[cantidadAristas];
        int k = 0;
        for (int i = 0; i < cantidad; i++)
            for (int j = i + 1; j < cantidad; j++) //solo el triángulo superior: así cada ruta sale una vez
                if (matriz[i, j] != SinArista)
                    rutas[k++] = new Ruta { Id = idsRutas[i, j], Origen = nombres[i], Destino = nombres[j], Tiempo = matriz[i, j] };
        return rutas;
    }

    // Auxiliares privados
    private void CrearMatriz(int capacidad) //matriz vacía: todo SinArista y la diagonal en 0
    {
        matriz = new int[capacidad, capacidad];
        idsRutas = new string[capacidad, capacidad];
        nombres = new string[capacidad];
        for (int i = 0; i < capacidad; i++)
            for (int j = 0; j < capacidad; j++)
                matriz[i, j] = i == j ? 0 : SinArista; //de un nodo a sí mismo hay 0 minutos
    }

    private void Ampliar() //duplica la capacidad copiando lo que ya había
    {
        int[,] matrizAnterior = matriz;
        string[,] idsAnteriores = idsRutas;
        string[] nombresAnteriores = nombres;
        CrearMatriz(Math.Max(4, nombres.Length * 2));
        for (int i = 0; i < cantidad; i++)
        {
            nombres[i] = nombresAnteriores[i];
            for (int j = 0; j < cantidad; j++)
            {
                matriz[i, j] = matrizAnterior[i, j];
                idsRutas[i, j] = idsAnteriores[i, j];
            }
        }
    }
}