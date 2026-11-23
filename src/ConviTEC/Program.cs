using System; //Console, Func, AppContext
using System.IO; //Path, File, Directory
using Microsoft.AspNetCore.Builder; //WebApplication
using ConviTEC.Api; //Endpoints, ContextoSede
using ConviTEC.Grafo; //GrafoMatriz, CargadorGrafo
using ConviTEC.Mensajeria; //ClienteMensajeria
using ConviTEC.Modelos; //Sede
using ConviTEC.Persistencia; //AlmacenSede
using ConviTEC.Servicios; //servicios

// Uso: dotnet run --project src/ConviTEC --no-launch-profile -- --sede ALA --puerto 5001 --rabbit localhost

string codigoSede = Argumento("--sede", "ALA").ToUpper(); //sede que representa esta instancia
int puerto = int.Parse(Argumento("--puerto", "5001")); //puerto de la web de esta sede
string hostRabbit = Argumento("--rabbit", "localhost"); //IP de la computadora con RabbitMQ
string usuarioRabbit = Argumento("--usuario", "convitec");
string claveRabbit = Argumento("--clave", "convitec");

string carpetaProyecto = Buscar(d => File.Exists(Path.Combine(d, "wwwroot", "index.html")), Path.Combine("src", "ConviTEC")); //donde está wwwroot
string carpetaConfig = Path.Combine(Buscar(d => File.Exists(Path.Combine(d, "config", "sedes.json")), ""), "config"); //config/ en la raíz del repo
string carpetaDatos = Path.Combine(Path.GetDirectoryName(carpetaConfig), "datos"); //datos/ junto a config/ (ignorado por Git)

var almacen = new AlmacenSede(carpetaConfig, carpetaDatos, codigoSede);
Sede[] sedes = Intentar(() => almacen.CargarSedes(), new[] { new Sede { Codigo = codigoSede, Nombre = codigoSede, Ubicacion = codigoSede } }, "cargar config/sedes.json");
Sede sede = null;
for (int i = 0; i < sedes.Length; i++) if (sedes[i].Codigo == codigoSede) sede = sedes[i]; //busca la sede pedida
if (sede == null) { Console.WriteLine($"La sede {codigoSede} no existe en config/sedes.json."); return; }
GrafoMatriz grafo = Intentar(() => CargadorGrafo.Cargar(Path.Combine(carpetaConfig, "rutas.json")), new GrafoMatriz(0), "cargar config/rutas.json");

var mensajeria = new ClienteMensajeria(hostRabbit, usuarioRabbit, claveRabbit, codigoSede); //arma los servicios y los conecta entre sí
var inventario = new ServicioInventario(sede, sedes, almacen);
var coordinador = new CoordinadorSedes(codigoSede, mensajeria, inventario);
inventario.Coordinador = coordinador;
mensajeria.AlRecibir = coordinador.Procesar;
var pedidos = new ServicioPedidos(sede, grafo, inventario, coordinador, almacen);
var ventas = new ServicioVentas(almacen);
var rutas = new ServicioRutas(sede, sedes, grafo, pedidos, ventas);

Intentar(() => { inventario.Cargar(); return true; }, false, "cargar el inventario"); //recupera la información guardada y reconstruye las estructuras
Intentar(() => { pedidos.Cargar(); return true; }, false, "cargar los pedidos");
Intentar(() => { ventas.Cargar(); return true; }, false, "cargar las ventas");
if (mensajeria.Conectar()) { Console.WriteLine($"Conectado a RabbitMQ en {hostRabbit}."); coordinador.SolicitarInventarios(); }
else Console.WriteLine($"Sin conexión con RabbitMQ en {hostRabbit}: la sede funciona sola.");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = carpetaProyecto });
var app = builder.Build();
app.UseDefaultFiles(); //en "/" sirve wwwroot/index.html
app.UseStaticFiles(); //sirve los archivos de wwwroot (frontend)
Endpoints.Mapear(app, new ContextoSede { Sede = sede, Mensajeria = mensajeria, Inventario = inventario, Pedidos = pedidos, Ventas = ventas, Rutas = rutas });
app.Lifetime.ApplicationStopping.Register(mensajeria.Cerrar); //cierra RabbitMQ al apagar
Console.WriteLine($"Sede {sede.Nombre} ({sede.Codigo}) lista en http://localhost:{puerto}");
app.Run($"http://0.0.0.0:{puerto}"); //escucha también desde otras computadoras de la red

string Argumento(string nombre, string porDefecto) //valor que sigue a "nombre" en la línea de comandos
{
    for (int i = 0; i < args.Length - 1; i++) if (args[i] == nombre) return args[i + 1];
    return porDefecto;
}

static string Buscar(Func<string, bool> cumple, string subcarpeta) //sube desde la carpeta actual hasta encontrar la ruta pedida
{
    foreach (string inicio in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        for (var d = new DirectoryInfo(inicio); d != null; d = d.Parent)
        {
            if (cumple(d.FullName)) return d.FullName;
            string sub = Path.Combine(d.FullName, subcarpeta);
            if (subcarpeta != "" && cumple(sub)) return sub;
        }
    throw new DirectoryNotFoundException("No se encontraron las carpetas del proyecto. Ejecute desde la raíz del repositorio.");
}

static T Intentar<T>(Func<T> accion, T alternativa, string tarea) //mientras un módulo no esté implementado, usa un valor temporal
{
    try { return accion(); }
    catch (NotImplementedException) { Console.WriteLine($"[pendiente] No se pudo {tarea}: falta implementar. Se usa un valor temporal."); return alternativa; }
}