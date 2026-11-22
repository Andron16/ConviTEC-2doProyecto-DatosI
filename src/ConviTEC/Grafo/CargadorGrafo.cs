using System; //NotImplementedException
using ConviTEC.Modelos; //Ruta

namespace ConviTEC.Grafo;

public class ArchivoRutas { public Ruta[] Rutas { get; set; } } //forma de config/rutas.json: { "rutas": [ ... ] }

public static class CargadorGrafo //construye el grafo a partir del archivo JSON
{
    public static GrafoMatriz Cargar(string rutaArchivo) => throw new NotImplementedException(); //lee el JSON, agrega nodos y aristas; lanza excepción si el archivo no existe o es inválido
}