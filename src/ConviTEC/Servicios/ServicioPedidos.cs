using System; //ArgumentException, DateTime
using ConviTEC.Estructuras; //ListaEnlazada, Cola
using ConviTEC.Grafo; //GrafoMatriz, Dijkstra, ResultadoDijkstra
using ConviTEC.Modelos; //Sede, Pedido, LineaPedido, SolicitudPedido, ResultadoPedido, Aporte, ProductoGlobal
using ConviTEC.Persistencia; //AlmacenSede

namespace ConviTEC.Servicios;

public class ServicioPedidos //registro de pedidos: valida existencias, reserva, verifica ruta, confirma o cancela
{
    private Sede _sede; //sede que recibe los pedidos
    private GrafoMatriz _grafo; //red de rutas, para verificar que el destino sea alcanzable
    private ServicioInventario _inventario; //reservas en la sede propia y existencias de la red
    private CoordinadorSedes _coordinador; //reservas en las otras sedes (por mensajes)
    private AlmacenSede _almacen; //guarda datos/<SEDE>/pedidos.json
    private ListaEnlazada<Pedido> _historial = new ListaEnlazada<Pedido>(); //todos los pedidos, del más antiguo al más reciente
    private Cola<Pedido> _pendientes = new Cola<Pedido>(); //pedidos por entregar, en orden de llegada
    private int _consecutivo; //último número usado en los Id de esta sede

    public ServicioPedidos(Sede sede, GrafoMatriz grafo, ServicioInventario inventario, CoordinadorSedes coordinador, AlmacenSede almacen) //guarda las dependencias
    {
        _sede = sede;
        _grafo = grafo;
        _inventario = inventario;
        _coordinador = coordinador;
        _almacen = almacen;
    }

    public void Cargar() //al arrancar: reconstruye el historial (lista) y los pendientes (cola) desde el archivo
    {
        lock (Sincronizacion.Candado)
        {
            Pedido[] pedidos = _almacen.CargarPedidos();
            for (int i = 0; i < pedidos.Length; i++)
            {
                _historial.Agregar(pedidos[i]);
                if (pedidos[i].Estado == EstadoPedido.Pendiente) _pendientes.Encolar(pedidos[i]);
                int numero = NumeroDe(pedidos[i].Id);
                if (numero > _consecutivo) _consecutivo = numero; //el siguiente Id continúa donde quedó
            }
        }
    }

    public ResultadoPedido Registrar(SolicitudPedido solicitud) //valida, reserva en las sedes, verifica la ruta y confirma o cancela
    {
        lock (Sincronizacion.Candado)
        {
            ValidarDestino(solicitud);
            ProductoGlobal[] productos = ValidarLineas(solicitud); //uno por línea, con sus unidades en cada sede
            Sede[] sedes = _inventario.CatalogoGlobal().Sedes;
            int propia = IndiceDeSede(sedes, _sede.Codigo);

            Pedido pedido = new Pedido { Id = NuevoId(), Fecha = DateTime.Now, Destino = solicitud.Destino, Lineas = ArmarLineas(productos, solicitud) };
            for (int i = 0; i < pedido.Lineas.Length; i++)
                pedido.Total += pedido.Lineas[i].Cantidad * pedido.Lineas[i].PrecioUnitario;

            // 1. ¿Alcanzan las existencias de toda la red?
            for (int i = 0; i < productos.Length; i++)
                if (productos[i].Total < pedido.Lineas[i].Cantidad)
                    return Rechazar(pedido, $"No hay existencias suficientes de {productos[i].Nombre}: se solicitaron {pedido.Lineas[i].Cantidad} y hay {productos[i].Total} disponibles en la red.");

            // 2. Reservar: primero en la sede propia y luego en las demás
            ListaEnlazada<Aporte> aportes = new ListaEnlazada<Aporte>(); //qué sede aporta qué
            ListaEnlazada<string> participantes = new ListaEnlazada<string>(); //sedes donde se intentó reservar
            for (int i = 0; i < productos.Length; i++)
            {
                int falta = pedido.Lineas[i].Cantidad;
                for (int k = 0; k < sedes.Length && falta > 0; k++)
                {
                    int s = (propia + k) % sedes.Length; //k = 0 es la sede propia; luego sigue con las demás
                    int toma = productos[i].Cantidades[s];
                    if (toma > falta) toma = falta;
                    if (toma == 0) continue; //esa sede no tiene unidades

                    AgregarSinRepetir(participantes, sedes[s].Codigo);
                    bool reservado;
                    string motivo = "no hay unidades suficientes.";
                    if (s == propia) reservado = _inventario.Reservar(pedido.Id, productos[i].Codigo, toma);
                    else reservado = _coordinador.ReservarRemoto(sedes[s].Codigo, pedido.Id, productos[i].Codigo, toma, out motivo);

                    if (!reservado)
                        return Cancelar(pedido, participantes, $"No se pudo reservar {productos[i].Nombre} en la sede {sedes[s].Nombre}: {motivo}");

                    aportes.Agregar(new Aporte { Sede = sedes[s].Codigo, Codigo = productos[i].Codigo, Nombre = productos[i].Nombre, Cantidad = toma });
                    falta -= toma;
                }
            }

            // 3. Verificar la ruta desde la sede hasta el destino
            ResultadoDijkstra rutas = Dijkstra.Calcular(_grafo, _sede.Ubicacion);
            if (!rutas.EsAlcanzable(pedido.Destino))
                return Cancelar(pedido, participantes, $"No existe ruta desde la sede {_sede.Nombre} hasta {pedido.Destino}.");

            // 4. Confirmar en todas las sedes y guardar como pendiente
            string[] codigos = participantes.ComoArreglo();
            for (int i = 0; i < codigos.Length; i++)
            {
                if (codigos[i] == _sede.Codigo) _inventario.Confirmar(pedido.Id);
                else _coordinador.ConfirmarRemoto(codigos[i], pedido.Id);
            }
            pedido.Estado = EstadoPedido.Pendiente;
            pedido.SedesParticipantes = codigos;
            _historial.Agregar(pedido);
            _pendientes.Encolar(pedido);
            Guardar();

            return new ResultadoPedido { Aceptado = true, Pedido = pedido, Aportes = aportes.ComoArreglo(), TiempoEstimado = rutas.TiempoHasta(pedido.Destino) };
        }
    }
    public Pedido[] Listar(string estado) //"todos" o un estado ("pendiente", "entregado"...); del más reciente al más antiguo
    {
        lock (Sincronizacion.Candado)
        {
            if (estado == null) estado = "todos";
            estado = estado.Trim().ToLower();
            Pedido[] todos = _historial.ComoArreglo();

            int cantidad = 0; //primero se cuentan, para crear el arreglo del tamaño justo
            for (int i = 0; i < todos.Length; i++)
                if (Cumple(todos[i], estado)) cantidad++;

            Pedido[] resultado = new Pedido[cantidad];
            int j = 0;
            for (int i = todos.Length - 1; i >= 0; i--) //de atrás hacia adelante: el más reciente primero
                if (Cumple(todos[i], estado)) resultado[j++] = todos[i];
            return resultado;
        }
    }

    public Pedido[] Pendientes() //pedidos por entregar, en orden de llegada
    {
        lock (Sincronizacion.Candado) { return _pendientes.ComoArreglo(); }
    }

    public void MarcarEntregados(Pedido[] entregados) //los saca de pendientes, cambia su estado y guarda
    {
        lock (Sincronizacion.Candado)
        {
            for (int i = 0; i < entregados.Length; i++)
            {
                string id = entregados[i].Id;
                Pedido guardado = _historial.Buscar(p => p.Id == id);
                if (guardado != null) guardado.Estado = EstadoPedido.Entregado;
            }

            Cola<Pedido> quedan = new Cola<Pedido>(); //se rearma la cola solo con los que siguen pendientes
            while (!_pendientes.EstaVacia)
            {
                Pedido p = _pendientes.Desencolar();
                if (p.Estado == EstadoPedido.Pendiente) quedan.Encolar(p);
            }
            _pendientes = quedan;
            Guardar();
        }
    }

    // Validaciones (lanzan ArgumentException, el endpoint responde 400)
    private void ValidarDestino(SolicitudPedido solicitud) //el destino debe existir en el grafo y no ser la propia sede
    {
        if (solicitud == null || solicitud.Destino == null || solicitud.Destino.Trim() == "")
            throw new ArgumentException("Seleccione un punto de entrega válido.");
        if (_grafo.IndiceDe(solicitud.Destino) == -1 || solicitud.Destino == _sede.Ubicacion)
            throw new ArgumentException("Seleccione un punto de entrega válido.");
    }

    private ProductoGlobal[] ValidarLineas(SolicitudPedido solicitud) //revisa cada línea y devuelve sus productos con las existencias por sede
    {
        if (solicitud.Lineas == null || solicitud.Lineas.Length == 0)
            throw new ArgumentException("El pedido debe tener al menos un producto.");

        ProductoGlobal[] productos = new ProductoGlobal[solicitud.Lineas.Length];
        for (int i = 0; i < solicitud.Lineas.Length; i++)
        {
            LineaSolicitud linea = solicitud.Lineas[i];
            if (linea == null || linea.Cantidad <= 0)
                throw new ArgumentException("Las cantidades deben ser mayores que 0.");

            CatalogoGlobal encontrado = linea.Codigo == null ? null : _inventario.BuscarEnCatalogo(linea.Codigo);
            if (encontrado == null)
                throw new ArgumentException($"No existe un producto con el código {linea.Codigo}.");
            productos[i] = encontrado.Productos[0];

            for (int j = 0; j < i; j++) //el mismo producto no puede venir en dos líneas
                if (productos[j].Codigo == productos[i].Codigo)
                    throw new ArgumentException($"El producto {productos[i].Codigo} está repetido en el pedido.");
        }
        return productos;
    }

    private static LineaPedido[] ArmarLineas(ProductoGlobal[] productos, SolicitudPedido solicitud) //detalle del pedido con nombre y precio del momento
    {
        LineaPedido[] lineas = new LineaPedido[productos.Length];
        for (int i = 0; i < productos.Length; i++)
            lineas[i] = new LineaPedido { Codigo = productos[i].Codigo, Nombre = productos[i].Nombre, Cantidad = solicitud.Lineas[i].Cantidad, PrecioUnitario = productos[i].Precio };
        return lineas;
    }

    // Finales de un pedido que no se acepta
    private ResultadoPedido Rechazar(Pedido pedido, string motivo) //no alcanzan las existencias: no se reservó nada
    {
        pedido.Estado = EstadoPedido.Rechazado;
        pedido.Motivo = motivo;
        pedido.SedesParticipantes = new string[0];
        _historial.Agregar(pedido);
        Guardar();
        return new ResultadoPedido { Aceptado = false, Motivo = motivo, ReservasLiberadas = new string[0] };
    }

    private ResultadoPedido Cancelar(Pedido pedido, ListaEnlazada<string> participantes, string motivo) //falló una reserva o no hay ruta: se libera todo lo reservado
    {
        string[] codigos = participantes.ComoArreglo();
        for (int i = 0; i < codigos.Length; i++)
        {
            if (codigos[i] == _sede.Codigo) _inventario.Liberar(pedido.Id);
            else _coordinador.LiberarRemoto(codigos[i], pedido.Id);
        }
        pedido.Estado = EstadoPedido.Cancelado;
        pedido.Motivo = motivo;
        pedido.SedesParticipantes = codigos;
        _historial.Agregar(pedido);
        Guardar();
        return new ResultadoPedido { Aceptado = false, Motivo = motivo, ReservasLiberadas = codigos };
    }

    // Auxiliares privados
    private string NuevoId() //"ALA-0001", "ALA-0002"...
    {
        _consecutivo++;
        return _sede.Codigo + "-" + _consecutivo.ToString("D4");
    }

    private int NumeroDe(string id) //"ALA-0007" → 7; 0 si el Id no es de esta sede
    {
        string prefijo = _sede.Codigo + "-";
        if (id == null || !id.StartsWith(prefijo)) return 0;
        int numero;
        if (int.TryParse(id.Substring(prefijo.Length), out numero)) return numero;
        return 0;
    }

    private static int IndiceDeSede(Sede[] sedes, string codigo) //posición de una sede en el arreglo; -1 si no está
    {
        for (int i = 0; i < sedes.Length; i++)
            if (sedes[i].Codigo == codigo) return i;
        return -1;
    }

    private static void AgregarSinRepetir(ListaEnlazada<string> lista, string valor) //agrega solo si todavía no está
    {
        if (lista.IndiceDe(x => x == valor) == -1) lista.Agregar(valor);
    }

    private static bool Cumple(Pedido p, string estado) //true si el pedido entra en el filtro
    {
        return estado == "todos" || p.Estado.ToString().ToLower() == estado;
    }

    private void Guardar() { _almacen.GuardarPedidos(_historial.ComoArreglo()); } //escribe datos/<SEDE>/pedidos.json
}