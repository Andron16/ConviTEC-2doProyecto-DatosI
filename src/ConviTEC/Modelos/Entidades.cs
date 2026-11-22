using System; //DateTime
using System.Text.Json.Serialization; //JsonStringEnumConverter

namespace ConviTEC.Modelos;

[JsonConverter(typeof(JsonStringEnumConverter))] //se guarda y se envía como texto ("Pendiente"), no como número
public enum EstadoPedido { Pendiente, Entregado, Cancelado, Rechazado } //estados posibles de un pedido

public class Sede //una sede de la cadena (config/sedes.json)
{
    public string Codigo { get; set; } //ej. "ALA"
    public string Nombre { get; set; } //ej. "Alajuela"
    public string Ubicacion { get; set; } //nombre del nodo en el grafo
}

public class Producto //producto del catálogo de ConviTEC
{
    public string Codigo { get; set; } //clave única, ej. "C001"
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public string Categoria { get; set; }
    public int Precio { get; set; } //precio de venta en colones
}

public class Existencia //producto en el inventario local (valor que guarda el AVL)
{
    public Producto Producto { get; set; }
    public int Disponible { get; set; } //unidades libres para vender
    public int Reservado { get; set; } //unidades apartadas por pedidos en curso
}

public class ExistenciaGlobal //copia del inventario de todas las sedes para un producto
{
    public string Codigo { get; set; }
    public int[] Cantidades { get; set; } //una por sede, en el orden de config/sedes.json
}

public class LineaPedido //un producto dentro de un pedido o venta
{
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public int Cantidad { get; set; }
    public int PrecioUnitario { get; set; } //precio al momento del pedido
}

public class Pedido //pedido registrado en una sede
{
    public string Id { get; set; } //"ALA-0001": código de sede + consecutivo
    public DateTime Fecha { get; set; } //fecha de creación
    public EstadoPedido Estado { get; set; }
    public string Destino { get; set; } //nodo del grafo donde se entrega
    public LineaPedido[] Lineas { get; set; }
    public int Total { get; set; } //suma de cantidad × precio unitario
    public string[] SedesParticipantes { get; set; } //sedes que aportaron productos
    public string Motivo { get; set; } //razón de cancelación o rechazo (null si no aplica)
}

public class Venta //se crea cuando un pedido se entrega
{
    public string PedidoId { get; set; }
    public DateTime Fecha { get; set; } //fecha de entrega
    public string Destino { get; set; }
    public LineaPedido[] Lineas { get; set; }
    public int Total { get; set; }
    public int TiempoTraslado { get; set; } //minutos acumulados del recorrido hasta esta entrega
}

public class Ruta //arista del grafo (una línea de config/rutas.json)
{
    public string Id { get; set; }
    public string Origen { get; set; }
    public string Destino { get; set; }
    public int Tiempo { get; set; } //minutos
}

public class Reserva //unidades apartadas en esta sede para un pedido (hasta confirmar o liberar)
{
    public string PedidoId { get; set; }
    public string Codigo { get; set; }
    public int Cantidad { get; set; }
}