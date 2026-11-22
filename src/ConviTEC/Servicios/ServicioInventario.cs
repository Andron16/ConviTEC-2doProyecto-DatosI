using System; //NotImplementedException
using ConviTEC.Modelos; //Sede, ProductoLocal, CatalogoGlobal, Producto
using ConviTEC.Persistencia; //AlmacenSede

namespace ConviTEC.Servicios;

public class ServicioInventario //inventario local (ArbolAVL) y copia del inventario global de todas las sedes
{
    public ServicioInventario(Sede sede, Sede[] sedes, AlmacenSede almacen) { } //solo guarda las dependencias (lo implementa D)

    public CoordinadorSedes Coordinador { get; set; } //se asigna en Program.cs; se usa para anunciar cambios a las demás sedes

    public void Cargar() { } //al arrancar: reconstruye el AVL y la copia global desde los archivos (lo implementa D)

    // Consultas para la web
    public ProductoLocal[] Inventario() => throw new NotImplementedException(); //recorrido en orden del AVL
    public ProductoLocal Buscar(string codigo) => throw new NotImplementedException(); //búsqueda en el AVL; null si no existe
    public ProductoLocal AgregarUnidades(string codigo, int cantidad) => throw new NotImplementedException(); //null si el producto no existe; ArgumentException si cantidad <= 0; guarda y anuncia
    public CatalogoGlobal CatalogoGlobal() => throw new NotImplementedException(); //todos los productos con existencias por sede
    public CatalogoGlobal BuscarEnCatalogo(string codigo) => throw new NotImplementedException(); //null si no existe
    public Producto ProductoDelCatalogo(string codigo) => throw new NotImplementedException(); //datos del producto; null si no existe

    // Copia global (la actualizan los mensajes de otras sedes)
    public int ExistenciaEn(string codigoSede, string codigo) => throw new NotImplementedException(); //unidades de un producto en una sede, según la copia global
    public void ActualizarCopiaGlobal(string codigoSede, string codigo, int cantidad) => throw new NotImplementedException(); //guarda el nuevo valor recibido

    // Reservas sobre el inventario LOCAL (las usa ServicioPedidos y, para pedidos de otras sedes, CoordinadorSedes)
    public bool Reservar(string pedidoId, string codigo, int cantidad) => throw new NotImplementedException(); //pasa de Disponible a Reservado; false si no alcanza
    public void Confirmar(string pedidoId) => throw new NotImplementedException(); //descuenta definitivamente lo reservado para ese pedido
    public void Liberar(string pedidoId) => throw new NotImplementedException(); //devuelve lo reservado al disponible
}