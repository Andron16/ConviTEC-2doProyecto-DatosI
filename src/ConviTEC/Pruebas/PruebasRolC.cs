using System; //DateTime
using System.Text.Json; //JsonSerializer
using System.Threading; //Thread
using ConviTEC.Mensajeria; //ClienteMensajeria, Mensaje, TipoMensaje
using ConviTEC.Persistencia; //AlmacenSede
using ConviTEC.Servicios; //CoordinadorSedes, ServicioInventario
using static ConviTEC.Pruebas.EjecutorPruebas; //Prueba(...), Omitir(...)

namespace ConviTEC.Pruebas;

public static class PruebasRolC //Abigail: mensajes entre sedes con RabbitMQ
{
    public static void Ejecutar(string host, string usuario, string clave) //registra y ejecuta todas las pruebas del rol
    {
        Prueba("Mensaje: viaja como JSON con el tipo en texto", () =>
        {
            Mensaje original = new Mensaje { Tipo = TipoMensaje.SolicitudReserva, SedeOrigen = "ALA", SedeDestino = "SJO", PedidoId = "ALA-0001", Codigo = "C001", Cantidad = 3 };
            string json = JsonSerializer.Serialize(original);
            Verificar.Verdadero(json.Contains("\"SolicitudReserva\""), "el tipo debe viajar como texto, no como número");
            Mensaje copia = JsonSerializer.Deserialize<Mensaje>(json);
            Verificar.Igual(original.Id, copia.Id, "Id");
            Verificar.Igual(TipoMensaje.SolicitudReserva, copia.Tipo, "Tipo");
            Verificar.Igual(3, copia.Cantidad, "Cantidad");
        });

        Prueba("ClienteMensajeria: sin servidor no se cae", () =>
        {
            ClienteMensajeria cliente = new ClienteMensajeria("servidor-inexistente.invalid", usuario, clave, "ALA");
            Verificar.Igual(false, cliente.Conectar(), "Conectar a un servidor inexistente");
            Verificar.Igual(false, cliente.Conectado, "Conectado");
            cliente.Enviar(new Mensaje { Tipo = TipoMensaje.Confirmar, SedeOrigen = "ALA", SedeDestino = "SJO" }); //no debe lanzar excepción
            cliente.Difundir(new Mensaje { Tipo = TipoMensaje.InventarioActualizado, SedeOrigen = "ALA" });
            Verificar.Nulo(cliente.EnviarYEsperar(new Mensaje { Tipo = TipoMensaje.SolicitudReserva, SedeOrigen = "ALA", SedeDestino = "SJO" }, 500), "EnviarYEsperar sin conexión");
            cliente.Cerrar();
        });

        Prueba("CoordinadorSedes: atiende reservas que pide otra sede", () =>
        {
            ServicioInventario inventario = InventarioALA();
            CoordinadorSedes coordinador = new CoordinadorSedes("ALA", new ClienteMensajeria("servidor-inexistente.invalid", usuario, clave, "ALA"), inventario);
            coordinador.Procesar(new Mensaje { Tipo = TipoMensaje.SolicitudReserva, SedeOrigen = "SJO", SedeDestino = "ALA", PedidoId = "SJO-0001", Codigo = "C001", Cantidad = 3 });
            Verificar.Igual(3, inventario.Buscar("C001").Reservado, "SolicitudReserva debe reservar en el inventario local");
            coordinador.Procesar(new Mensaje { Tipo = TipoMensaje.Confirmar, SedeOrigen = "SJO", SedeDestino = "ALA", PedidoId = "SJO-0001" });
            Verificar.Igual(0, inventario.Buscar("C001").Reservado, "Confirmar debe descontar lo reservado");
            Verificar.Igual(7, inventario.Buscar("C001").Disponible, "disponible tras confirmar (10 − 3)");

            coordinador.Procesar(new Mensaje { Tipo = TipoMensaje.SolicitudReserva, SedeOrigen = "LIM", SedeDestino = "ALA", PedidoId = "LIM-0001", Codigo = "C001", Cantidad = 2 });
            coordinador.Procesar(new Mensaje { Tipo = TipoMensaje.Liberar, SedeOrigen = "LIM", SedeDestino = "ALA", PedidoId = "LIM-0001" });
            coordinador.Procesar(new Mensaje { Tipo = TipoMensaje.Liberar, SedeOrigen = "LIM", SedeDestino = "ALA", PedidoId = "LIM-0001" }); //mensaje repetido
            Verificar.Igual(7, inventario.Buscar("C001").Disponible, "Liberar debe devolver las unidades una sola vez");
        });

        Prueba("CoordinadorSedes: actualiza la copia global con los avisos de otras sedes", () =>
        {
            ServicioInventario inventario = InventarioALA();
            CoordinadorSedes coordinador = new CoordinadorSedes("ALA", new ClienteMensajeria("servidor-inexistente.invalid", usuario, clave, "ALA"), inventario);
            coordinador.Procesar(new Mensaje { Tipo = TipoMensaje.InventarioActualizado, SedeOrigen = "SJO", Codigo = "C001", Cantidad = 9 });
            Verificar.Igual(9, inventario.ExistenciaEn("SJO", "C001"), "existencia de SJO en la copia global");
        });

        Prueba("CoordinadorSedes: una sede sin respuesta rechaza la reserva remota", () =>
        {
            CoordinadorSedes coordinador = new CoordinadorSedes("ALA", new ClienteMensajeria("servidor-inexistente.invalid", usuario, clave, "ALA"), InventarioALA());
            string motivo;
            Verificar.Igual(false, coordinador.ReservarRemoto("SJO", "ALA-0001", "C001", 1, out motivo), "ReservarRemoto sin conexión");
            Verificar.NoNulo(motivo, "debe explicar el motivo");
        });

        Prueba("RabbitMQ: solicitud y respuesta entre dos sedes", () =>
        {
            ClienteMensajeria ala = new ClienteMensajeria(host, usuario, clave, "ALA");
            ClienteMensajeria sjo = new ClienteMensajeria(host, usuario, clave, "SJO");
            if (!ala.Conectar() || !sjo.Conectar()) Omitir($"no hay RabbitMQ en {host} con el usuario {usuario}");
            try
            {
                sjo.AlRecibir = m => //SJO responde a cada solicitud de reserva
                {
                    if (m.Tipo == TipoMensaje.SolicitudReserva)
                        sjo.Enviar(new Mensaje { Tipo = TipoMensaje.RespuestaReserva, RespuestaA = m.Id, SedeOrigen = "SJO", SedeDestino = m.SedeOrigen, Aceptado = true });
                };
                Mensaje respuesta = ala.EnviarYEsperar(new Mensaje { Tipo = TipoMensaje.SolicitudReserva, SedeOrigen = "ALA", SedeDestino = "SJO", PedidoId = "ALA-0001", Codigo = "C001", Cantidad = 1 }, 3000);
                Verificar.NoNulo(respuesta, "ALA debe recibir la respuesta de SJO");
                Verificar.Verdadero(respuesta.Aceptado, "la respuesta debe venir aceptada");
            }
            finally { ala.Cerrar(); sjo.Cerrar(); }
        });

        Prueba("RabbitMQ: difusión y tiempo de espera", () =>
        {
            ClienteMensajeria ala = new ClienteMensajeria(host, usuario, clave, "ALA");
            ClienteMensajeria sjo = new ClienteMensajeria(host, usuario, clave, "SJO");
            if (!ala.Conectar() || !sjo.Conectar()) Omitir($"no hay RabbitMQ en {host} con el usuario {usuario}");
            try
            {
                int recibidosALA = 0, recibidosSJO = 0;
                ala.AlRecibir = m => { if (m.Tipo == TipoMensaje.InventarioActualizado) recibidosALA++; };
                sjo.AlRecibir = m => { if (m.Tipo == TipoMensaje.InventarioActualizado) recibidosSJO++; };
                ala.Difundir(new Mensaje { Tipo = TipoMensaje.InventarioActualizado, SedeOrigen = "ALA", Codigo = "C001", Cantidad = 5 });
                for (int i = 0; i < 30 && recibidosSJO == 0; i++) Thread.Sleep(100); //espera hasta 3 s
                Verificar.Igual(1, recibidosSJO, "SJO debe recibir la difusión");
                Verificar.Igual(0, recibidosALA, "ALA no debe recibir su propio mensaje");

                DateTime inicio = DateTime.Now; //nadie responde por LIM: debe agotarse el tiempo
                Mensaje sinRespuesta = ala.EnviarYEsperar(new Mensaje { Tipo = TipoMensaje.SolicitudReserva, SedeOrigen = "ALA", SedeDestino = "LIM" }, 1000);
                Verificar.Nulo(sinRespuesta, "EnviarYEsperar a una sede que no responde");
                Verificar.Verdadero((DateTime.Now - inicio).TotalMilliseconds >= 900, "debe esperar el tiempo indicado antes de devolver null");
            }
            finally { ala.Cerrar(); sjo.Cerrar(); }
        });
    }

    private static ServicioInventario InventarioALA() //inventario de Alajuela con los datos de prueba (C001 = 10 disponibles)
    {
        AlmacenSede almacen = new AlmacenSede(DatosPrueba.CrearConfig(), DatosPrueba.CarpetaTemporal(), "ALA");
        ServicioInventario inventario = new ServicioInventario(DatosPrueba.Sedes()[1], DatosPrueba.Sedes(), almacen);
        inventario.Cargar();
        return inventario;
    }
}
