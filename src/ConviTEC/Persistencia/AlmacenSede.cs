using System; //NotImplementedException
using System.IO; //Path
using ConviTEC.Modelos; //Sede, Producto, Existencia, Pedido, Venta...

namespace ConviTEC.Persistencia;

public class ArchivoSedes { public Sede[] Sedes { get; set; } } //forma de config/sedes.json
public class ArchivoProductos { public Producto[] Productos { get; set; } } //forma de config/productos.json
public class CantidadInicial { public string Codigo { get; set; } public int Cantidad { get; set; } } //existencia inicial de un producto
public class InventarioInicialSede { public string Sede { get; set; } public CantidadInicial[] Existencias { get; set; } } //inventario inicial de una sede
public class ArchivoInventarioInicial { public InventarioInicialSede[] Inventarios { get; set; } } //forma de config/inventario-inicial.json

public class AlmacenSede //lee la configuración compartida y lee/escribe los archivos propios de una sede en datos/<SEDE>/
{
    public string CarpetaConfig { get; } //ej. "config"
    public string CarpetaSede { get; } //ej. "datos/ALA"

    public AlmacenSede(string carpetaConfig, string carpetaDatos, string codigoSede) //solo guarda las rutas
    {
        CarpetaConfig = carpetaConfig;
        CarpetaSede = Path.Combine(carpetaDatos, codigoSede);
    }

    // Configuración compartida (solo lectura)
    public Sede[] CargarSedes() => throw new NotImplementedException(); //config/sedes.json
    public Producto[] CargarProductos() => throw new NotImplementedException(); //config/productos.json

    // Datos propios de la sede (se crean la primera vez)
    public Existencia[] CargarInventario() => throw new NotImplementedException(); //si no existe inventario.json, lo arma con productos.json + inventario-inicial.json
    public void GuardarInventario(Existencia[] existencias) => throw new NotImplementedException();
    public ExistenciaGlobal[] CargarCopiaGlobal() => throw new NotImplementedException(); //arreglo vacío si no existe
    public void GuardarCopiaGlobal(ExistenciaGlobal[] copia) => throw new NotImplementedException();
    public Pedido[] CargarPedidos() => throw new NotImplementedException(); //arreglo vacío si no existe
    public void GuardarPedidos(Pedido[] pedidos) => throw new NotImplementedException();
    public Venta[] CargarVentas() => throw new NotImplementedException(); //arreglo vacío si no existe
    public void GuardarVentas(Venta[] ventas) => throw new NotImplementedException();
    public Reserva[] CargarReservas() => throw new NotImplementedException(); //reservas en curso; arreglo vacío si no existe
    public void GuardarReservas(Reserva[] reservas) => throw new NotImplementedException();
}