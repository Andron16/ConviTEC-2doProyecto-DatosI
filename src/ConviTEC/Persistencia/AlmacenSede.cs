using System; //Console, Exception
using System.IO; //Path, File, Directory
using System.Text.Encodings.Web; //JavaScriptEncoder
using System.Text.Json; //JsonSerializer
using ConviTEC.Modelos; //Sede, Producto, Existencia, Pedido, Venta...

namespace ConviTEC.Persistencia;

public class ArchivoSedes { public Sede[] Sedes { get; set; } } //forma de config/sedes.json
public class ArchivoProductos { public Producto[] Productos { get; set; } } //forma de config/productos.json
public class CantidadInicial { public string Codigo { get; set; } public int Cantidad { get; set; } } //existencia inicial de un producto
public class InventarioInicialSede { public string Sede { get; set; } public CantidadInicial[] Existencias { get; set; } } //inventario inicial de una sede
public class ArchivoInventarioInicial { public InventarioInicialSede[] Inventarios { get; set; } } //forma de config/inventario-inicial.json

public class AlmacenSede //lee la configuración compartida y lee/escribe los archivos propios de una sede en datos/<SEDE>/
{
    private static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true, //"codigo" del JSON llena la propiedad Codigo
        WriteIndented = true, //archivos legibles
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping //guarda "San José" tal cual y no "San José"
    };

    private string _codigoSede; //para elegir el inventario inicial de esta sede

    public string CarpetaConfig { get; } //ej. "config"
    public string CarpetaSede { get; } //ej. "datos/ALA"

    public AlmacenSede(string carpetaConfig, string carpetaDatos, string codigoSede) //solo guarda las rutas
    {
        CarpetaConfig = carpetaConfig;
        CarpetaSede = Path.Combine(carpetaDatos, codigoSede);
        _codigoSede = codigoSede;
    }

    // Configuración compartida (solo lectura)
    public Sede[] CargarSedes() //config/sedes.json
    {
        ArchivoSedes archivo = Leer<ArchivoSedes>(Path.Combine(CarpetaConfig, "sedes.json"));
        return archivo == null || archivo.Sedes == null ? new Sede[0] : archivo.Sedes;
    }

    public Producto[] CargarProductos() //config/productos.json
    {
        ArchivoProductos archivo = Leer<ArchivoProductos>(Path.Combine(CarpetaConfig, "productos.json"));
        return archivo == null || archivo.Productos == null ? new Producto[0] : archivo.Productos;
    }

    // Datos propios de la sede (se crean la primera vez)
    public Existencia[] CargarInventario() //si no existe inventario.json, lo arma con productos.json + inventario-inicial.json
    {
        Existencia[] guardado = Leer<Existencia[]>(ArchivoSede("inventario.json"));
        if (guardado != null) return guardado;

        Producto[] productos = CargarProductos(); //primera vez: un registro por producto del catálogo
        CantidadInicial[] iniciales = InventarioInicialDeEstaSede();
        Existencia[] existencias = new Existencia[productos.Length];
        for (int i = 0; i < productos.Length; i++)
        {
            productos[i].Codigo = productos[i].Codigo.Trim().ToUpper(); //convención: códigos en mayúscula
            existencias[i] = new Existencia { Producto = productos[i], Disponible = CantidadInicialDe(iniciales, productos[i].Codigo), Reservado = 0 };
        }
        GuardarInventario(existencias);
        return existencias;
    }

    public void GuardarInventario(Existencia[] existencias) => Escribir("inventario.json", existencias);

    public ExistenciaGlobal[] CargarCopiaGlobal() => Leer<ExistenciaGlobal[]>(ArchivoSede("copia-global.json")) ?? new ExistenciaGlobal[0]; //arreglo vacío si no existe
    public void GuardarCopiaGlobal(ExistenciaGlobal[] copia) => Escribir("copia-global.json", copia);

    public Pedido[] CargarPedidos() => Leer<Pedido[]>(ArchivoSede("pedidos.json")) ?? new Pedido[0]; //arreglo vacío si no existe
    public void GuardarPedidos(Pedido[] pedidos) => Escribir("pedidos.json", pedidos);

    public Venta[] CargarVentas() => Leer<Venta[]>(ArchivoSede("ventas.json")) ?? new Venta[0]; //arreglo vacío si no existe
    public void GuardarVentas(Venta[] ventas) => Escribir("ventas.json", ventas);

    public Reserva[] CargarReservas() => Leer<Reserva[]>(ArchivoSede("reservas.json")) ?? new Reserva[0]; //reservas en curso; arreglo vacío si no existe
    public void GuardarReservas(Reserva[] reservas) => Escribir("reservas.json", reservas);

    // Auxiliares privados
    private string ArchivoSede(string nombre) => Path.Combine(CarpetaSede, nombre); //ruta dentro de datos/<SEDE>/

    private static T Leer<T>(string ruta) where T : class //deserializa el archivo; null si no existe o está dañado
    {
        if (!File.Exists(ruta)) return null;
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(ruta), Opciones); }
        catch (JsonException e) { Console.WriteLine($"[aviso] El archivo {ruta} está dañado y no se pudo leer: {e.Message}"); return null; }
    }

    private void Escribir<T>(string nombre, T datos) //serializa y escribe; crea datos/<SEDE>/ si no existe
    {
        Directory.CreateDirectory(CarpetaSede); //no hace nada si ya existe
        File.WriteAllText(ArchivoSede(nombre), JsonSerializer.Serialize(datos, Opciones));
    }

    private CantidadInicial[] InventarioInicialDeEstaSede() //existencias iniciales de esta sede; vacío si no aparece
    {
        ArchivoInventarioInicial archivo = Leer<ArchivoInventarioInicial>(Path.Combine(CarpetaConfig, "inventario-inicial.json"));
        if (archivo == null || archivo.Inventarios == null) return new CantidadInicial[0];
        for (int i = 0; i < archivo.Inventarios.Length; i++)
            if (archivo.Inventarios[i].Sede == _codigoSede && archivo.Inventarios[i].Existencias != null)
                return archivo.Inventarios[i].Existencias;
        return new CantidadInicial[0];
    }

    private static int CantidadInicialDe(CantidadInicial[] iniciales, string codigo) //búsqueda lineal; 0 si el producto no tiene cantidad inicial
    {
        for (int i = 0; i < iniciales.Length; i++)
            if (iniciales[i].Codigo != null && iniciales[i].Codigo.Trim().ToUpper() == codigo)
                return iniciales[i].Cantidad < 0 ? 0 : iniciales[i].Cantidad; //una cantidad negativa en el archivo se toma como 0
        return 0;
    }
}