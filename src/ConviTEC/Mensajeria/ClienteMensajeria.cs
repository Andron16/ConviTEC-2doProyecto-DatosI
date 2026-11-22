using System; //Action

namespace ConviTEC.Mensajeria;

// Mientras C no lo implemente, se comporta como si no hubiera conexión:
// no envía nada y EnviarYEsperar devuelve null (como si la otra sede no respondiera).
// Así el resto del sistema puede probarse sin RabbitMQ.
public class ClienteMensajeria //conexión de esta sede con el intermediario de mensajes (RabbitMQ)
{
    public ClienteMensajeria(string host, string usuario, string clave, string codigoSede) { } //solo guarda los datos (lo implementa C)

    public bool Conectado => false; //true si la conexión con RabbitMQ está activa
    public Action<Mensaje> AlRecibir { get; set; } //lo asigna D: qué hacer con cada mensaje que llega (excepto las respuestas que se están esperando)

    public bool Conectar() => false; //abre la conexión y la cola de esta sede; false si no se pudo
    public void Enviar(Mensaje mensaje) { } //envía a la cola de mensaje.SedeDestino
    public void Difundir(Mensaje mensaje) { } //envía a todas las demás sedes
    public Mensaje EnviarYEsperar(Mensaje mensaje, int milisegundos) => null; //envía y espera la respuesta (RespuestaA == mensaje.Id); null si no llega a tiempo
    public void Cerrar() { } //cierra la conexión al apagar la sede
}