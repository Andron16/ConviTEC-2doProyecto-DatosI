using ConviTEC.Mensajeria; //ClienteMensajeria, Mensaje

namespace ConviTEC.Servicios;

// Mientras C no lo implemente, se comporta como si las otras sedes no respondieran.
public class CoordinadorSedes //protocolo entre sedes: atiende mensajes que llegan y hace solicitudes a otras sedes
{
    public CoordinadorSedes(string codigoSede, ClienteMensajeria mensajeria, ServicioInventario inventario) { } //solo guarda las dependencias (lo implementa C)

    public void Procesar(Mensaje mensaje) { } //se asigna a mensajeria.AlRecibir: atiende SolicitudReserva, Confirmar, Liberar, InventarioActualizado y SolicitudInventario

    // Solicitudes de esta sede hacia otra
    public bool ReservarRemoto(string sede, string pedidoId, string codigo, int cantidad, out string motivo) //true si la otra sede apartó las unidades
    {
        motivo = "La sede no respondió."; //temporal: se comporta como sede sin respuesta
        return false;
    }
    public void ConfirmarRemoto(string sede, string pedidoId) { } //la otra sede descuenta lo reservado
    public void LiberarRemoto(string sede, string pedidoId) { } //la otra sede devuelve lo reservado

    // Avisos a todas las sedes
    public void AnunciarExistencia(string codigo, int cantidad) { } //difunde InventarioActualizado con la existencia local nueva
    public void SolicitarInventarios() { } //al arrancar: pide a las demás sedes sus existencias
}