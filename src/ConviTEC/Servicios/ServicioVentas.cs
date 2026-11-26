using System; //ArgumentException, DateTime, Console, Exception
using ConviTEC.Estructuras; //ListaEnlazada, ArbolAVL, Ordenamiento
using ConviTEC.Modelos; //Pedido, Venta, Estadisticas, ProductoEstrella, PedidoRendimiento
using ConviTEC.Persistencia; //AlmacenSede

namespace ConviTEC.Servicios;

public class ServicioVentas //historial de ventas y estadísticas de la sede
{
    private const int CantidadPeores = 5; //cuántos peores pedidos se muestran
    private AlmacenSede _almacen; //lee y guarda datos/<SEDE>/ventas.json
    private ListaEnlazada<Venta> _historial = new ListaEnlazada<Venta>(); //de la más reciente a la más antigua

    public ServicioVentas(AlmacenSede almacen) { _almacen = almacen; } //solo guarda la dependencia

    public void Cargar() //al arrancar: reconstruye el historial desde el archivo (nunca lanza excepción)
    {
        lock (Sincronizacion.Candado)
        {
            try
            {
                Venta[] ventas = _almacen.CargarVentas(); //el archivo ya está de la más reciente a la más antigua
                for (int i = 0; i < ventas.Length; i++) _historial.Agregar(ventas[i]);
            }
            catch (Exception e) { Console.WriteLine($"[aviso] No se pudieron cargar las ventas: {e.Message}"); }
        }
    }

    public void RegistrarEntrega(Pedido pedido, int tiempoTraslado) //crea la Venta y guarda
    {
        lock (Sincronizacion.Candado)
        {
            if (pedido == null) throw new ArgumentException("No se indicó el pedido entregado.");
            if (tiempoTraslado < 0) throw new ArgumentException("El tiempo de traslado no puede ser negativo.");
            if (_historial.Buscar(v => v.PedidoId == pedido.Id) != null) return; //ya estaba registrado: no se duplica la venta

            _historial.AgregarAlInicio(new Venta //al inicio: así el historial queda del más reciente al más antiguo
            {
                PedidoId = pedido.Id,
                Fecha = DateTime.Now, //fecha de entrega
                Destino = pedido.Destino,
                Lineas = pedido.Lineas ?? new LineaPedido[0],
                Total = pedido.Total,
                TiempoTraslado = tiempoTraslado
            });
            _almacen.GuardarVentas(_historial.ComoArreglo());
        }
    }

    public Venta[] Historial() //de la más reciente a la más antigua
    {
        lock (Sincronizacion.Candado) { return _historial.ComoArreglo(); }
    }

    public Estadisticas Calcular() //ingresos, producto estrella y 5 peores pedidos (ordenados con Ordenamiento)
    {
        lock (Sincronizacion.Candado)
        {
            Venta[] ventas = _historial.ComoArreglo();
            int ingresos = 0;
            for (int i = 0; i < ventas.Length; i++) ingresos += ventas[i].Total;

            return new Estadisticas
            {
                IngresosTotales = ingresos,
                PedidosEntregados = ventas.Length,
                ProductoEstrella = BuscarProductoEstrella(ventas),
                PeoresPedidos = PeoresPedidos(ventas)
            };
        }
    }

    // Auxiliares privados
    private static ProductoEstrella BuscarProductoEstrella(Venta[] ventas) //producto con más ingresos (unidades × precio); null si no hay ventas
    {
        ArbolAVL<string, ProductoEstrella> acumulados = new ArbolAVL<string, ProductoEstrella>(); //un acumulado por código de producto
        for (int i = 0; i < ventas.Length; i++)
        {
            LineaPedido[] lineas = ventas[i].Lineas;
            if (lineas == null) continue;
            for (int j = 0; j < lineas.Length; j++)
            {
                if (lineas[j] == null || lineas[j].Codigo == null) continue; //línea inválida: se ignora
                string codigo = lineas[j].Codigo.Trim().ToUpper();
                ProductoEstrella p = acumulados.Buscar(codigo);
                if (p == null) //primera vez que aparece este producto
                {
                    p = new ProductoEstrella { Codigo = codigo, Nombre = lineas[j].Nombre, PrecioUnitario = lineas[j].PrecioUnitario };
                    acumulados.Insertar(codigo, p);
                }
                p.Unidades += lineas[j].Cantidad;
                p.Ingresos += lineas[j].Cantidad * lineas[j].PrecioUnitario;
            }
        }

        ProductoEstrella[] productos = acumulados.EnOrden();
        ProductoEstrella mejor = null;
        for (int i = 0; i < productos.Length; i++) //búsqueda del máximo; en empate gana el de código menor
            if (mejor == null || productos[i].Ingresos > mejor.Ingresos) mejor = productos[i];
        return mejor;
    }

    private static PedidoRendimiento[] PeoresPedidos(Venta[] ventas) //los 5 de menor rendimiento, de menor a mayor
    {
        PedidoRendimiento[] todos = new PedidoRendimiento[ventas.Length];
        for (int i = 0; i < ventas.Length; i++)
        {
            int tiempo = ventas[i].TiempoTraslado;
            double rendimiento = tiempo > 0 ? (double)ventas[i].Total / tiempo : ventas[i].Total; //tiempo 0: se usa el monto (evita dividir entre 0)
            todos[i] = new PedidoRendimiento { Id = ventas[i].PedidoId, Monto = ventas[i].Total, Tiempo = tiempo, Rendimiento = Math.Round(rendimiento, 2) };
        }

        Ordenamiento.Ordenar(todos, (a, b) => a.Rendimiento.CompareTo(b.Rendimiento)); //merge sort ascendente

        int cantidad = todos.Length < CantidadPeores ? todos.Length : CantidadPeores;
        PedidoRendimiento[] peores = new PedidoRendimiento[cantidad];
        for (int i = 0; i < cantidad; i++) peores[i] = todos[i];
        return peores;
    }
}