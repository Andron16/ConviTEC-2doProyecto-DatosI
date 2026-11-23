using System; //ArgumentException, Predicate
using ConviTEC.Estructuras; //ArbolAVL, ListaEnlazada
using ConviTEC.Modelos; //Sede, Existencia, ExistenciaGlobal, Reserva, ProductoLocal, ProductoGlobal, CatalogoGlobal, Producto
using ConviTEC.Persistencia; //AlmacenSede

namespace ConviTEC.Servicios;

public class ServicioInventario //inventario local (ArbolAVL) y copia del inventario global de todas las sedes
{
    private Sede[] _sedes; //todas las sedes, en el orden de config/sedes.json
    private AlmacenSede _almacen; //lee y guarda los archivos de la sede
    private int _indiceSede; //posición de esta sede en _sedes (y en cada arreglo Cantidades)
    private ArbolAVL<string, Existencia> _arbol = new ArbolAVL<string, Existencia>(); //inventario local, clave = código del producto
    private ArbolAVL<string, ExistenciaGlobal> _copia = new ArbolAVL<string, ExistenciaGlobal>(); //unidades de cada producto en cada sede
    private ListaEnlazada<Reserva> _reservas = new ListaEnlazada<Reserva>(); //unidades apartadas para pedidos en curso

    public ServicioInventario(Sede sede, Sede[] sedes, AlmacenSede almacen) //guarda las dependencias y ubica esta sede en la lista
    {
        _sedes = sedes;
        _almacen = almacen;
        _indiceSede = IndiceDeSede(sede.Codigo);
    }

    public CoordinadorSedes Coordinador { get; set; } //se asigna en Program.cs; se usa para anunciar cambios a las demás sedes

    public void Cargar() //al arrancar: reconstruye el AVL, las reservas y la copia global desde los archivos
    {
        lock (Sincronizacion.Candado)
        {
            Existencia[] existencias = _almacen.CargarInventario();
            for (int i = 0; i < existencias.Length; i++)
                _arbol.Insertar(existencias[i].Producto.Codigo, existencias[i]);

            Reserva[] reservas = _almacen.CargarReservas();
            for (int i = 0; i < reservas.Length; i++)
                _reservas.Agregar(reservas[i]);

            ExistenciaGlobal[] copia = _almacen.CargarCopiaGlobal();
            for (int i = 0; i < copia.Length; i++)
                _copia.Insertar(copia[i].Codigo, copia[i]);

            for (int i = 0; i < existencias.Length; i++) //productos que todavía no tienen fila en la copia global
            {
                string codigo = existencias[i].Producto.Codigo;
                if (!_copia.Contiene(codigo))
                    _copia.Insertar(codigo, new ExistenciaGlobal { Codigo = codigo, Cantidades = new int[_sedes.Length] });
            }
        }
    }

    // Consultas para la web
    public ProductoLocal[] Inventario() //todos los productos de la sede, ordenados por código (recorrido en orden del AVL)
    {
        lock (Sincronizacion.Candado)
        {
            Existencia[] existencias = _arbol.EnOrden();
            ProductoLocal[] resultado = new ProductoLocal[existencias.Length];
            for (int i = 0; i < existencias.Length; i++)
                resultado[i] = ALocal(existencias[i]);
            return resultado;
        }
    }

    public ProductoLocal Buscar(string codigo) //un producto de la sede; null si no existe
    {
        lock (Sincronizacion.Candado)
        {
            Existencia e = BuscarExistencia(codigo);
            if (e == null) return null;
            return ALocal(e);
        }
    }

    public ProductoLocal AgregarUnidades(string codigo, int cantidad) //suma unidades al disponible, guarda y anuncia
    {
        lock (Sincronizacion.Candado)
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor que cero.");
            Existencia e = BuscarExistencia(codigo);
            if (e == null) return null; //el endpoint lo convierte en 404
            e.Disponible += cantidad;
            GuardarInventario();
            Anunciar(e);
            return ALocal(e);
        }
    }

    public CatalogoGlobal CatalogoGlobal() //todos los productos con sus unidades en cada sede
    {
        lock (Sincronizacion.Candado)
        {
            Existencia[] existencias = _arbol.EnOrden();
            ProductoGlobal[] productos = new ProductoGlobal[existencias.Length];
            for (int i = 0; i < existencias.Length; i++)
                productos[i] = AGlobal(existencias[i]);
            return new CatalogoGlobal { Sedes = _sedes, Productos = productos };
        }
    }

    public CatalogoGlobal BuscarEnCatalogo(string codigo) //mismo formato que el catálogo, con un solo producto; null si no existe
    {
        lock (Sincronizacion.Candado)
        {
            Existencia e = BuscarExistencia(codigo);
            if (e == null) return null;
            return new CatalogoGlobal { Sedes = _sedes, Productos = new ProductoGlobal[] { AGlobal(e) } };
        }
    }

    public Producto ProductoDelCatalogo(string codigo) //datos del producto (nombre, precio...); null si no existe
    {
        lock (Sincronizacion.Candado)
        {
            Existencia e = BuscarExistencia(codigo);
            if (e == null) return null;
            return e.Producto;
        }
    }

    // Copia global (la actualizan los mensajes de otras sedes)
    public int ExistenciaEn(string codigoSede, string codigo) //unidades disponibles de un producto en una sede
    {
        lock (Sincronizacion.Candado)
        {
            int indice = IndiceDeSede(codigoSede);
            if (indice == -1) return 0; //sede desconocida

            if (indice == _indiceSede) //la sede propia siempre se lee del AVL
            {
                Existencia e = BuscarExistencia(codigo);
                if (e == null) return 0;
                return e.Disponible;
            }

            ExistenciaGlobal fila = BuscarFila(codigo); //otra sede: se lee de la copia global
            if (fila == null) return 0;
            return fila.Cantidades[indice];
        }
    }

    public void ActualizarCopiaGlobal(string codigoSede, string codigo, int cantidad) //guarda el valor que anunció otra sede
    {
        lock (Sincronizacion.Candado)
        {
            int indice = IndiceDeSede(codigoSede);
            if (indice == -1 || indice == _indiceSede) return; //sede desconocida o la propia: se ignora

            codigo = Normalizar(codigo);
            if (codigo == null) return;

            ExistenciaGlobal fila = _copia.Buscar(codigo);
            if (fila == null) //producto que aún no estaba en la copia
            {
                fila = new ExistenciaGlobal { Codigo = codigo, Cantidades = new int[_sedes.Length] };
                _copia.Insertar(codigo, fila);
            }
            fila.Cantidades[indice] = cantidad;
            _almacen.GuardarCopiaGlobal(_copia.EnOrden());
        }
    }

    // Reservas sobre el inventario LOCAL (las usa ServicioPedidos y, para pedidos de otras sedes, CoordinadorSedes)
    public bool Reservar(string pedidoId, string codigo, int cantidad) //pasa unidades de Disponible a Reservado; false si no alcanza
    {
        lock (Sincronizacion.Candado)
        {
            Existencia e = BuscarExistencia(codigo);
            if (e == null || cantidad <= 0 || e.Disponible < cantidad) return false;

            e.Disponible -= cantidad;
            e.Reservado += cantidad;
            _reservas.Agregar(new Reserva { PedidoId = pedidoId, Codigo = e.Producto.Codigo, Cantidad = cantidad });

            GuardarInventario();
            GuardarReservas();
            Anunciar(e); //el disponible bajó
            return true;
        }
    }

    public void Confirmar(string pedidoId) //el pedido se aceptó: lo reservado se descuenta definitivamente
    {
        lock (Sincronizacion.Candado) { QuitarReservas(pedidoId, false); }
    }

    public void Liberar(string pedidoId) //el pedido se canceló: lo reservado vuelve al disponible
    {
        lock (Sincronizacion.Candado) { QuitarReservas(pedidoId, true); }
    }

    private void QuitarReservas(string pedidoId, bool devolver) //quita todas las reservas del pedido; devolver = true las regresa al disponible
    {
        bool huboCambios = false;
        int i = _reservas.IndiceDe(r => r.PedidoId == pedidoId);
        while (i != -1) //un pedido puede tener una reserva por cada producto
        {
            Reserva reserva = _reservas.Obtener(i);
            Existencia e = _arbol.Buscar(reserva.Codigo);
            if (e != null)
            {
                e.Reservado -= reserva.Cantidad;
                if (devolver)
                {
                    e.Disponible += reserva.Cantidad;
                    Anunciar(e); //el disponible subió
                }
            }
            _reservas.EliminarEn(i);
            huboCambios = true;
            i = _reservas.IndiceDe(r => r.PedidoId == pedidoId);
        }
        if (huboCambios) //si el pedido no tenía reservas, no se toca nada
        {
            GuardarInventario();
            GuardarReservas();
        }
    }
    // Auxiliares privados
    private int IndiceDeSede(string codigoSede) //posición de una sede en _sedes; -1 si no existe
    {
        for (int i = 0; i < _sedes.Length; i++)
            if (_sedes[i].Codigo == codigoSede) return i;
        return -1;
    }

    private static string Normalizar(string codigo) //" c001" → "C001"; null si no hay código
    {
        if (codigo == null) return null;
        return codigo.Trim().ToUpper();
    }

    private Existencia BuscarExistencia(string codigo) //existencia local de un producto; null si no existe
    {
        codigo = Normalizar(codigo);
        if (codigo == null) return null;
        return _arbol.Buscar(codigo);
    }

    private ExistenciaGlobal BuscarFila(string codigo) //fila de la copia global de un producto; null si no existe
    {
        codigo = Normalizar(codigo);
        if (codigo == null) return null;
        return _copia.Buscar(codigo);
    }

    private static ProductoLocal ALocal(Existencia e) //convierte una existencia al formato de la API
    {
        return new ProductoLocal
        {
            Codigo = e.Producto.Codigo,
            Nombre = e.Producto.Nombre,
            Descripcion = e.Producto.Descripcion,
            Categoria = e.Producto.Categoria,
            Precio = e.Producto.Precio,
            Disponible = e.Disponible,
            Reservado = e.Reservado
        };
    }

    private ProductoGlobal AGlobal(Existencia e) //producto con sus unidades en cada sede y el total
    {
        int[] cantidades = new int[_sedes.Length]; //arreglo nuevo: la web no recibe el de la copia interna
        ExistenciaGlobal fila = _copia.Buscar(e.Producto.Codigo);
        if (fila != null)
            for (int i = 0; i < cantidades.Length && i < fila.Cantidades.Length; i++)
                cantidades[i] = fila.Cantidades[i];
        if (_indiceSede != -1) cantidades[_indiceSede] = e.Disponible; //la columna propia siempre sale del AVL

        int total = 0;
        for (int i = 0; i < cantidades.Length; i++)
            total += cantidades[i];

        return new ProductoGlobal
        {
            Codigo = e.Producto.Codigo,
            Nombre = e.Producto.Nombre,
            Descripcion = e.Producto.Descripcion,
            Categoria = e.Producto.Categoria,
            Precio = e.Producto.Precio,
            Cantidades = cantidades,
            Total = total
        };
    }

    private void GuardarInventario() { _almacen.GuardarInventario(_arbol.EnOrden()); } //escribe datos/<SEDE>/inventario.json

    private void GuardarReservas() { _almacen.GuardarReservas(_reservas.ComoArreglo()); } //escribe datos/<SEDE>/reservas.json

    private void Anunciar(Existencia e) //avisa a las demás sedes el nuevo disponible de este producto
    {
        if (Coordinador != null) Coordinador.AnunciarExistencia(e.Producto.Codigo, e.Disponible);
    }
}