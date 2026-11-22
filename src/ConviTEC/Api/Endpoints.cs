using System; //Func, excepciones
using Microsoft.AspNetCore.Builder; //WebApplication, MapGet, MapPost
using Microsoft.AspNetCore.Http; //Results, IResult
using ConviTEC.Mensajeria; //ClienteMensajeria
using ConviTEC.Modelos; //Sede, EstadoSede, solicitudes y respuestas
using ConviTEC.Servicios; //servicios de la sede

namespace ConviTEC.Api;

public class ContextoSede //todo lo que necesitan los endpoints de esta sede
{
    public Sede Sede { get; set; }
    public ClienteMensajeria Mensajeria { get; set; }
    public ServicioInventario Inventario { get; set; }
    public ServicioPedidos Pedidos { get; set; }
    public ServicioVentas Ventas { get; set; }
    public ServicioRutas Rutas { get; set; }
}

public static class Endpoints //rutas REST del contrato con la web (solo traducen HTTP ↔ servicios, sin lógica)
{
    public static void Mapear(WebApplication app, ContextoSede c) //registra todas las rutas
    {
        app.MapGet("/api/sede", () => Ejecutar(() => new EstadoSede { Codigo = c.Sede.Codigo, Nombre = c.Sede.Nombre, Ubicacion = c.Sede.Ubicacion, Conectado = c.Mensajeria.Conectado }));

        app.MapGet("/api/inventario", () => Ejecutar(() => c.Inventario.Inventario()));
        app.MapGet("/api/inventario/{codigo}", (string codigo) => Ejecutar(() => c.Inventario.Buscar(codigo), $"No existe un producto con el código {codigo}."));
        app.MapPost("/api/inventario/unidades", (SolicitudUnidades s) => Ejecutar(() => c.Inventario.AgregarUnidades(s.Codigo, s.Cantidad), $"No existe un producto con el código {s.Codigo}."));

        app.MapGet("/api/catalogo", () => Ejecutar(() => c.Inventario.CatalogoGlobal()));
        app.MapGet("/api/catalogo/{codigo}", (string codigo) => Ejecutar(() => c.Inventario.BuscarEnCatalogo(codigo), $"No existe un producto con el código {codigo}."));

        app.MapGet("/api/destinos", () => Ejecutar(() => c.Rutas.Destinos()));
        app.MapPost("/api/pedidos", (SolicitudPedido s) => Ejecutar(() => c.Pedidos.Registrar(s)));
        app.MapGet("/api/pedidos", (string estado) => Ejecutar(() => c.Pedidos.Listar(estado ?? "todos")));

        app.MapGet("/api/grafo", () => Ejecutar(() => c.Rutas.Grafo()));
        app.MapGet("/api/recorrido", () => Ejecutar(() => c.Rutas.Estado()));
        app.MapPost("/api/recorrido/iniciar", (SolicitudRecorrido s) => Ejecutar(() => c.Rutas.Iniciar(s.Salida)));
        app.MapPost("/api/recorrido/siguiente", () => Ejecutar(() => c.Rutas.EntregarSiguiente()));

        app.MapGet("/api/ventas", () => Ejecutar(() => c.Ventas.Historial()));
        app.MapGet("/api/estadisticas", () => Ejecutar(() => c.Ventas.Calcular()));
    }

    private static IResult Ejecutar(Func<object> accion, string siNoExiste = "No se encontró lo solicitado.") //ejecuta con el candado y traduce el resultado a HTTP
    {
        try
        {
            object resultado;
            lock (Sincronizacion.Candado) { resultado = accion(); }
            return resultado == null ? Results.NotFound(new RespuestaError { Error = siNoExiste }) : Results.Ok(resultado); //null = 404
        }
        catch (NotImplementedException) { return Results.Json(new RespuestaError { Error = "Esta función todavía no está implementada en el backend." }, statusCode: 501); }
        catch (ArgumentException e) { return Results.BadRequest(new RespuestaError { Error = e.Message }); } //datos inválidos
        catch (InvalidOperationException e) { return Results.BadRequest(new RespuestaError { Error = e.Message }); } //operación no permitida en este momento
        catch (Exception e) { Console.WriteLine(e); return Results.Json(new RespuestaError { Error = $"Error interno: {e.Message}" }, statusCode: 500); }
    }
}