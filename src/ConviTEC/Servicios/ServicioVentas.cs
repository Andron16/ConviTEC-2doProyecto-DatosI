using System; //NotImplementedException
using ConviTEC.Modelos; //Pedido, Venta, Estadisticas
using ConviTEC.Persistencia; //AlmacenSede

namespace ConviTEC.Servicios;

public class ServicioVentas //historial de ventas y estadísticas de la sede
{
    public ServicioVentas(AlmacenSede almacen) { } //solo guarda la dependencia (lo implementa A)

    public void Cargar() { } //al arrancar: reconstruye el historial desde el archivo (lo implementa A)

    public void RegistrarEntrega(Pedido pedido, int tiempoTraslado) => throw new NotImplementedException(); //crea la Venta y guarda
    public Venta[] Historial() => throw new NotImplementedException(); //de la más reciente a la más antigua
    public Estadisticas Calcular() => throw new NotImplementedException(); //ingresos, producto estrella y 5 peores pedidos (ordenados con Ordenamiento)
}