using System; //NotImplementedException
using ConviTEC.Grafo; //GrafoMatriz
using ConviTEC.Modelos; //Sede, Pedido, SolicitudPedido, ResultadoPedido
using ConviTEC.Persistencia; //AlmacenSede

namespace ConviTEC.Servicios;

public class ServicioPedidos //registro de pedidos: valida existencias, reserva, verifica ruta, confirma o cancela
{
    public ServicioPedidos(Sede sede, GrafoMatriz grafo, ServicioInventario inventario, CoordinadorSedes coordinador, AlmacenSede almacen) { } //solo guarda las dependencias (lo implementa D)

    public void Cargar() { } //al arrancar: reconstruye el historial (lista) y los pendientes (cola) desde el archivo (lo implementa D)

    public ResultadoPedido Registrar(SolicitudPedido solicitud) => throw new NotImplementedException(); //ArgumentException si la solicitud es inválida (sin líneas, cantidad <= 0, destino inexistente)
    public Pedido[] Listar(string estado) => throw new NotImplementedException(); //"todos" o un estado ("pendiente", "entregado"...); del más reciente al más antiguo
    public Pedido[] Pendientes() => throw new NotImplementedException(); //pedidos por entregar, en orden de llegada
    public void MarcarEntregados(Pedido[] entregados) => throw new NotImplementedException(); //los saca de pendientes, cambia su estado y guarda
}