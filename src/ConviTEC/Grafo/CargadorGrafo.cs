using System; //Console, InvalidOperationException
using System.IO; //File
using System.Text.Json; //JsonSerializer
using ConviTEC.Modelos; //Ruta

namespace ConviTEC.Grafo;

public class ArchivoRutas { public Ruta[] Rutas { get; set; } } //forma de config/rutas.json: { "rutas": [ ... ] }

public static class CargadorGrafo //construye el grafo a partir del archivo JSON
{
    private static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; //"origen" del JSON llena Origen

    public static GrafoMatriz Cargar(string rutaArchivo) //lee el JSON, agrega nodos y aristas; lanza excepción si el archivo no existe o es inválido
    {
        string texto = File.ReadAllText(rutaArchivo); //FileNotFoundException si no existe
        ArchivoRutas archivo = JsonSerializer.Deserialize<ArchivoRutas>(texto, Opciones); //JsonException si el formato está dañado
        if (archivo == null || archivo.Rutas == null) throw new InvalidOperationException($"El archivo {rutaArchivo} no tiene la lista de rutas.");

        GrafoMatriz grafo = new GrafoMatriz(archivo.Rutas.Length * 2); //cada ruta aporta como máximo 2 nodos nuevos
        for (int i = 0; i < archivo.Rutas.Length; i++)
        {
            Ruta r = archivo.Rutas[i];
            if (r == null || r.Origen == null || r.Destino == null) //ruta incompleta: se avisa y se ignora
            {
                Console.WriteLine($"[aviso] La ruta #{i + 1} de {rutaArchivo} está incompleta y se ignoró.");
                continue;
            }
            grafo.AgregarNodo(r.Origen);
            grafo.AgregarNodo(r.Destino);
            if (!grafo.AgregarArista(r.Origen, r.Destino, r.Tiempo)) //tiempo <= 0 o origen = destino
                Console.WriteLine($"[aviso] La ruta {r.Id} ({r.Origen} - {r.Destino}) no es válida y se ignoró.");
        }
        return grafo;
    }
}