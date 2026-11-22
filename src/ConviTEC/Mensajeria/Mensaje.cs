using System; //DateTime
using System.Text.Json.Serialization; //JsonStringEnumConverter

namespace ConviTEC.Mensajeria;

[JsonConverter(typeof(JsonStringEnumConverter))] //viaja como texto ("SolicitudReserva")
public enum TipoMensaje //tipos de mensaje entre sedes
{
    SolicitudReserva,      //"apártame N unidades de este producto para el pedido X"
    RespuestaReserva,      //respuesta a una solicitud: Aceptado true/false
    Confirmar,             //el pedido se aceptó: descontar definitivamente lo reservado
    Liberar,               //el pedido se canceló: devolver lo reservado al disponible
    InventarioActualizado, //aviso a todas las sedes: "mi existencia de este producto ahora es N"
    SolicitudInventario    //al arrancar: "envíenme sus existencias" (cada sede responde con InventarioActualizado)
}

public class Mensaje //lo que viaja por RabbitMQ (serializado a JSON)
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); //identificador único del mensaje
    public string RespuestaA { get; set; } //Id del mensaje que se está respondiendo (solo en respuestas)
    public TipoMensaje Tipo { get; set; }
    public string SedeOrigen { get; set; } //código de quien envía
    public string SedeDestino { get; set; } //código de quien recibe; null si es para todas
    public string PedidoId { get; set; }
    public string Codigo { get; set; } //código de producto
    public int Cantidad { get; set; }
    public bool Aceptado { get; set; } //en RespuestaReserva
    public string Texto { get; set; } //motivo de un rechazo u otro detalle
    public DateTime Fecha { get; set; } = DateTime.Now;
}