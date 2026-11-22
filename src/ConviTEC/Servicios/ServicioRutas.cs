using System; //NotImplementedException
using ConviTEC.Grafo; //GrafoMatriz
using ConviTEC.Modelos; //Sede, GrafoRespuesta, EstadoRecorrido

namespace ConviTEC.Servicios;

public class ServicioRutas //grafo para la web y recorrido de entregas de la sede
{
    public ServicioRutas(Sede sede, Sede[] sedes, GrafoMatriz grafo, ServicioPedidos pedidos, ServicioVentas ventas) { } //solo guarda las dependencias (lo implementa B)

    public GrafoRespuesta Grafo() => throw new NotImplementedException(); //nodos (marcando cuáles son sedes) y aristas
    public string[] Destinos() => throw new NotImplementedException(); //nodos que se pueden elegir como destino (todos menos la sede)
    public EstadoRecorrido Estado() => throw new NotImplementedException(); //recorrido en curso, o pendientes con su tiempo desde la sede si no hay recorrido
    public EstadoRecorrido Iniciar(string salida) => throw new NotImplementedException(); //ArgumentException si la salida no es "11:00" ni "15:00"
    public EstadoRecorrido EntregarSiguiente() => throw new NotImplementedException(); //entrega, registra ventas y marca entregados; InvalidOperationException si no hay recorrido activo
}