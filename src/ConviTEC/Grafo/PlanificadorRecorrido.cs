using System; //NotImplementedException
using ConviTEC.Modelos; //Pedido, EstadoRecorrido

namespace ConviTEC.Grafo;

public class PlanificadorRecorrido //recorrido voraz: en cada paso va al destino pendiente más cercano (Dijkstra)
{
    public PlanificadorRecorrido(GrafoMatriz grafo, string origen) => throw new NotImplementedException(); //origen = ubicación de la sede

    public bool Activo => throw new NotImplementedException(); //true mientras haya un recorrido iniciado
    public int TiempoAcumulado => throw new NotImplementedException(); //minutos desde la salida (para Venta.TiempoTraslado)

    public EstadoRecorrido Iniciar(string salida, Pedido[] pendientes) => throw new NotImplementedException(); //arranca en la sede con los pedidos dados
    public EstadoRecorrido Estado() => throw new NotImplementedException(); //estado del recorrido en curso (Activo = false si no hay)
    public Pedido[] EntregarSiguiente() => throw new NotImplementedException(); //viaja al destino más cercano y devuelve los pedidos entregados ahí; vacío si no queda nada alcanzable
}