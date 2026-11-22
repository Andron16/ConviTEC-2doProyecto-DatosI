namespace ConviTEC.Servicios;

public static class Sincronizacion //un solo candado por sede: evita que una petición web y un mensaje de otra sede modifiquen el inventario al mismo tiempo
{
    public static readonly object Candado = new object();
}