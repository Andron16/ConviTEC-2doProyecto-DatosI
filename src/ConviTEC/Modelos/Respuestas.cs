namespace ConviTEC.Modelos;

// Clases con la forma exacta del contrato de la API (lo que recibe y devuelve la web)

public class EstadoSede { public string Codigo { get; set; } public string Nombre { get; set; } public string Ubicacion { get; set; } public bool Conectado { get; set; } } //GET /api/sede

public class ProductoLocal //GET /api/inventario
{
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public string Categoria { get; set; }
    public int Precio { get; set; }
    public int Disponible { get; set; }
    public int Reservado { get; set; }
}

public class SolicitudUnidades { public string Codigo { get; set; } public int Cantidad { get; set; } } //POST /api/inventario/unidades

public class ProductoGlobal //un producto del catálogo global
{
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public string Categoria { get; set; }
    public int Precio { get; set; }
    public int[] Cantidades { get; set; } //en el mismo orden que CatalogoGlobal.Sedes
    public int Total { get; set; }
}

public class CatalogoGlobal { public Sede[] Sedes { get; set; } public ProductoGlobal[] Productos { get; set; } } //GET /api/catalogo

public class LineaSolicitud { public string Codigo { get; set; } public int Cantidad { get; set; } } //línea pedida desde la web

public class SolicitudPedido { public string Destino { get; set; } public LineaSolicitud[] Lineas { get; set; } } //POST /api/pedidos

public class Aporte { public string Sede { get; set; } public string Codigo { get; set; } public string Nombre { get; set; } public int Cantidad { get; set; } } //qué sede aportó qué

public class ResultadoPedido //respuesta de POST /api/pedidos
{
    public bool Aceptado { get; set; }
    public Pedido Pedido { get; set; } //solo si fue aceptado
    public Aporte[] Aportes { get; set; }
    public int TiempoEstimado { get; set; } //minutos desde la sede hasta el destino
    public string Motivo { get; set; } //solo si fue rechazado
    public string[] ReservasLiberadas { get; set; } //sedes donde se liberaron reservas
}

public class SolicitudRecorrido { public string Salida { get; set; } } //POST /api/recorrido/iniciar

public class NodoGrafo { public string Nombre { get; set; } public string Sede { get; set; } } //Sede = código o null

public class GrafoRespuesta { public NodoGrafo[] Nodos { get; set; } public Ruta[] Aristas { get; set; } } //GET /api/grafo

public class ProductoEstrella { public string Codigo { get; set; } public string Nombre { get; set; } public int Unidades { get; set; } public int PrecioUnitario { get; set; } public int Ingresos { get; set; } }

public class PedidoRendimiento { public string Id { get; set; } public int Monto { get; set; } public int Tiempo { get; set; } public double Rendimiento { get; set; } } //monto / tiempo

public class Estadisticas //GET /api/estadisticas
{
    public int IngresosTotales { get; set; }
    public int PedidosEntregados { get; set; }
    public ProductoEstrella ProductoEstrella { get; set; } //null si no hay ventas
    public PedidoRendimiento[] PeoresPedidos { get; set; } //de menor a mayor rendimiento
}

public class RespuestaError { public string Error { get; set; } } //cuerpo de cualquier error 4xx