using System; //ArgumentException
using System.IO; //Path
using ConviTEC.Grafo; //GrafoMatriz, CargadorGrafo
using ConviTEC.Mensajeria; //ClienteMensajeria
using ConviTEC.Modelos; //Sede, SolicitudPedido, LineaSolicitud, ResultadoPedido, Pedido, CatalogoGlobal, EstadoRecorrido
using ConviTEC.Persistencia; //AlmacenSede
using ConviTEC.Servicios; //ServicioInventario, ServicioPedidos, CoordinadorSedes, ServicioVentas, ServicioRutas
using static ConviTEC.Pruebas.EjecutorPruebas; //Prueba(...)

namespace ConviTEC.Pruebas;

public static class PruebasRolD //Andron: inventario, pedidos e integración de todos los módulos
{
    private static string _rutas; //config/rutas.json real (tabla del enunciado)

    public static void Ejecutar(string carpetaConfig) //registra y ejecuta todas las pruebas del rol
    {
        _rutas = Path.Combine(carpetaConfig, "rutas.json");

        // ServicioInventario
        Prueba("ServicioInventario: carga, orden por código y búsqueda", () =>
        {
            ServicioInventario inventario = new Sistema("ALA").Inventario;
            Verificar.Arreglo(new[] { "A003", "C001", "G014" }, Codigos(inventario.Inventario()), "inventario ordenado por código (recorrido en orden del AVL)");
            Verificar.Igual(10, inventario.Buscar(" c001").Disponible, "Buscar normaliza el código");
            Verificar.Nulo(inventario.Buscar("Z999"), "Buscar un producto inexistente");
        });

        Prueba("ServicioInventario: agregar unidades y recuperarlas al reiniciar", () =>
        {
            Sistema sistema = new Sistema("ALA");
            Verificar.Igual(15, sistema.Inventario.AgregarUnidades("C001", 5).Disponible, "disponible tras agregar 5");
            Verificar.Lanza<ArgumentException>(() => sistema.Inventario.AgregarUnidades("C001", 0), "agregar 0 unidades");
            Verificar.Nulo(sistema.Inventario.AgregarUnidades("Z999", 5), "agregar a un producto inexistente");
            Verificar.Igual(15, sistema.Reiniciar().Inventario.Buscar("C001").Disponible, "disponible después de reiniciar");
        });

        Prueba("ServicioInventario: reservar, confirmar y liberar", () =>
        {
            Sistema sistema = new Sistema("ALA");
            ServicioInventario inventario = sistema.Inventario;
            Verificar.Verdadero(inventario.Reservar("ALA-0001", "C001", 4), "reservar 4");
            Verificar.Igual(6, inventario.Buscar("C001").Disponible, "disponible tras reservar");
            Verificar.Igual(4, inventario.Buscar("C001").Reservado, "reservado tras reservar");
            Verificar.Igual(false, inventario.Reservar("ALA-0002", "C001", 20), "reservar más de lo disponible");
            inventario.Confirmar("ALA-0001");
            Verificar.Igual(0, inventario.Buscar("C001").Reservado, "reservado tras confirmar");
            Verificar.Igual(6, inventario.Buscar("C001").Disponible, "disponible tras confirmar");

            inventario.Reservar("ALA-0003", "C001", 2);
            ServicioInventario reiniciado = sistema.Reiniciar().Inventario; //la reserva debe sobrevivir al reinicio
            reiniciado.Liberar("ALA-0003");
            reiniciado.Liberar("ALA-0003"); //mensaje repetido: no debe devolver dos veces
            Verificar.Igual(6, reiniciado.Buscar("C001").Disponible, "disponible tras liberar");
            Verificar.Igual(0, reiniciado.Buscar("C001").Reservado, "reservado tras liberar");
        });

        Prueba("ServicioInventario: copia global y catálogo", () =>
        {
            ServicioInventario inventario = new Sistema("ALA").Inventario;
            inventario.ActualizarCopiaGlobal("SJO", "C001", 7);
            inventario.ActualizarCopiaGlobal("ALA", "C001", 999); //la sede propia se ignora: su dato real está en el AVL
            Verificar.Igual(7, inventario.ExistenciaEn("SJO", "C001"), "existencia en SJO");
            Verificar.Igual(10, inventario.ExistenciaEn("ALA", "C001"), "existencia en ALA");
            CatalogoGlobal cafe = inventario.BuscarEnCatalogo("C001");
            Verificar.Arreglo(new[] { 0, 10, 7, 0 }, cafe.Productos[0].Cantidades, "cantidades por sede (LIB, ALA, SJO, LIM)");
            Verificar.Igual(17, cafe.Productos[0].Total, "total en la red");
            Verificar.Igual(3, inventario.CatalogoGlobal().Productos.Length, "productos del catálogo");
            Verificar.Nulo(inventario.BuscarEnCatalogo("Z999"), "producto inexistente en el catálogo");
        });

        // ServicioPedidos
        Prueba("ServicioPedidos: pedido local aceptado", () =>
        {
            Sistema sistema = new Sistema("ALA");
            ResultadoPedido r = sistema.Pedidos.Registrar(Solicitud("Heredia", "C001", 3));
            Verificar.Verdadero(r.Aceptado, "el pedido debe aceptarse: " + r.Motivo);
            Verificar.Igual("ALA-0001", r.Pedido.Id, "Id");
            Verificar.Igual(EstadoPedido.Pendiente, r.Pedido.Estado, "Estado");
            Verificar.Igual(8400, r.Pedido.Total, "Total (3 × 2800)");
            Verificar.Igual(20, r.TiempoEstimado, "TiempoEstimado Alajuela → Heredia");
            Verificar.Igual(1, r.Aportes.Length, "aportes");
            Verificar.Igual(7, sistema.Inventario.Buscar("C001").Disponible, "disponible tras el pedido");
            Verificar.Igual(1, sistema.Pedidos.Pendientes().Length, "pendientes");
        });

        Prueba("ServicioPedidos: solicitudes inválidas no crean pedidos", () =>
        {
            ServicioPedidos pedidos = new Sistema("ALA").Pedidos;
            Verificar.Lanza<ArgumentException>(() => pedidos.Registrar(Solicitud("Marte", "C001", 1)), "destino inexistente");
            Verificar.Lanza<ArgumentException>(() => pedidos.Registrar(Solicitud("Alajuela", "C001", 1)), "destino igual a la sede");
            Verificar.Lanza<ArgumentException>(() => pedidos.Registrar(new SolicitudPedido { Destino = "Heredia", Lineas = new LineaSolicitud[0] }), "pedido sin líneas");
            Verificar.Lanza<ArgumentException>(() => pedidos.Registrar(Solicitud("Heredia", "C001", 0)), "cantidad 0");
            Verificar.Lanza<ArgumentException>(() => pedidos.Registrar(Solicitud("Heredia", "Z999", 1)), "producto inexistente");
            SolicitudPedido repetido = new SolicitudPedido { Destino = "Heredia", Lineas = new[] { new LineaSolicitud { Codigo = "C001", Cantidad = 1 }, new LineaSolicitud { Codigo = "c001", Cantidad = 1 } } };
            Verificar.Lanza<ArgumentException>(() => pedidos.Registrar(repetido), "producto repetido");
            Verificar.Igual(0, pedidos.Listar("todos").Length, "no debe quedar ningún pedido en el historial");
            Verificar.Igual("ALA-0001", pedidos.Registrar(Solicitud("Heredia", "C001", 1)).Pedido.Id, "las solicitudes inválidas no gastan Id");
        });

        Prueba("ServicioPedidos: rechazado por existencias insuficientes en la red", () =>
        {
            Sistema sistema = new Sistema("ALA");
            ResultadoPedido r = sistema.Pedidos.Registrar(Solicitud("Heredia", "A003", 1)); //ALA tiene 0 y no conoce existencias de otras sedes
            Verificar.Igual(false, r.Aceptado, "Aceptado");
            Verificar.NoNulo(r.Motivo, "Motivo");
            Verificar.Igual(1, sistema.Pedidos.Listar("rechazado").Length, "el rechazo queda en el historial");
            Verificar.Igual(0, sistema.Pedidos.Pendientes().Length, "no debe quedar pendiente");
        });

        Prueba("ServicioPedidos: cancelado si otra sede no responde, y se libera lo reservado", () =>
        {
            Sistema sistema = new Sistema("ALA");
            sistema.Inventario.ActualizarCopiaGlobal("SJO", "C001", 7); //ALA cree que SJO tiene 7
            ResultadoPedido r = sistema.Pedidos.Registrar(Solicitud("Heredia", "C001", 12)); //10 de ALA + 2 de SJO (que no responde)
            Verificar.Igual(false, r.Aceptado, "Aceptado");
            Verificar.Arreglo(new[] { "ALA", "SJO" }, r.ReservasLiberadas, "sedes donde se liberaron reservas");
            Verificar.Igual(10, sistema.Inventario.Buscar("C001").Disponible, "las 10 unidades de ALA deben volver al disponible");
            Verificar.Igual(0, sistema.Inventario.Buscar("C001").Reservado, "no debe quedar nada reservado");
            Verificar.Igual(1, sistema.Pedidos.Listar("cancelado").Length, "la cancelación queda en el historial");
        });

        Prueba("ServicioPedidos: cancelado si no hay ruta al destino (Liberia → Cartago)", () =>
        {
            Sistema sistema = new Sistema("LIB");
            ResultadoPedido r = sistema.Pedidos.Registrar(Solicitud("Cartago", "C001", 1));
            Verificar.Igual(false, r.Aceptado, "Aceptado");
            Verificar.Verdadero(r.Motivo != null && r.Motivo.Contains("ruta"), "el motivo debe mencionar la ruta");
            Verificar.Igual(6, sistema.Inventario.Buscar("C001").Disponible, "la unidad reservada en LIB debe volver");
        });

        Prueba("ServicioPedidos: historial, filtros, entregas y reinicio", () =>
        {
            Sistema sistema = new Sistema("ALA");
            sistema.Pedidos.Registrar(Solicitud("Heredia", "C001", 1));  //ALA-0001
            sistema.Pedidos.Registrar(Solicitud("Cartago", "G014", 1));  //ALA-0002
            sistema.Pedidos.Registrar(Solicitud("Heredia", "A003", 1));  //ALA-0003 rechazado
            Verificar.Arreglo(new[] { "ALA-0003", "ALA-0002", "ALA-0001" }, Ids(sistema.Pedidos.Listar("todos")), "historial del más reciente al más antiguo");
            Verificar.Igual(2, sistema.Pedidos.Listar("PENDIENTE").Length, "el filtro no distingue mayúsculas");

            sistema.Pedidos.MarcarEntregados(new[] { sistema.Pedidos.Pendientes()[0] });
            Verificar.Arreglo(new[] { "ALA-0002" }, Ids(sistema.Pedidos.Pendientes()), "pendientes tras entregar ALA-0001");
            Verificar.Igual(1, sistema.Pedidos.Listar("entregado").Length, "entregados");

            ServicioPedidos reiniciado = sistema.Reiniciar().Pedidos;
            Verificar.Arreglo(new[] { "ALA-0002" }, Ids(reiniciado.Pendientes()), "pendientes después de reiniciar");
            Verificar.Igual("ALA-0004", reiniciado.Registrar(Solicitud("Heredia", "C001", 1)).Pedido.Id, "el consecutivo continúa después de reiniciar");
        });

        // Integración de todos los módulos
        Prueba("Integración: pedido → recorrido → ventas y estadísticas", () =>
        {
            Sistema sistema = new Sistema("ALA");
            sistema.Pedidos.Registrar(Solicitud("Cartago", "G014", 1)); //950
            sistema.Pedidos.Registrar(Solicitud("Heredia", "C001", 2)); //5600
            Verificar.Lanza<ArgumentException>(() => sistema.Rutas.Iniciar("10:00"), "salida distinta de 11:00 y 15:00");

            EstadoRecorrido estado = sistema.Rutas.Iniciar("11:00");
            Verificar.Igual("Heredia", estado.Siguiente.Destino, "primer destino (el más cercano)");
            sistema.Rutas.EntregarSiguiente();
            estado = sistema.Rutas.EntregarSiguiente();
            Verificar.Igual(85, estado.TiempoTotal, "tiempo total (20 + 65)");
            Verificar.Igual(0, sistema.Pedidos.Pendientes().Length, "no deben quedar pendientes");
            Verificar.Igual(2, sistema.Pedidos.Listar("entregado").Length, "los dos pedidos deben quedar entregados");

            Venta[] ventas = sistema.Ventas.Historial();
            Verificar.Igual(2, ventas.Length, "una venta por pedido entregado");
            Verificar.Igual("ALA-0001", ventas[0].PedidoId, "la venta más reciente es la de Cartago");
            Verificar.Igual(85, ventas[0].TiempoTraslado, "tiempo de traslado acumulado hasta Cartago");
            Verificar.Igual(6550, sistema.Ventas.Calcular().IngresosTotales, "ingresos totales");
        });
    }

    private class Sistema //una sede armada igual que en Program.cs, con config de prueba y una carpeta de datos temporal
    {
        private string _codigo, _config, _datos;
        public ServicioInventario Inventario { get; private set; }
        public ServicioPedidos Pedidos { get; private set; }
        public ServicioVentas Ventas { get; private set; }
        public ServicioRutas Rutas { get; private set; }

        public Sistema(string codigoSede) : this(codigoSede, DatosPrueba.CrearConfig(), DatosPrueba.CarpetaTemporal()) { } //sede nueva con datos vacíos

        private Sistema(string codigoSede, string config, string datos) //arma y conecta los servicios
        {
            _codigo = codigoSede;
            _config = config;
            _datos = datos;
            Sede[] sedes = DatosPrueba.Sedes();
            Sede sede = null;
            for (int i = 0; i < sedes.Length; i++) if (sedes[i].Codigo == codigoSede) sede = sedes[i];

            AlmacenSede almacen = new AlmacenSede(config, datos, codigoSede);
            GrafoMatriz grafo;
            try { grafo = CargadorGrafo.Cargar(_rutas); }
            catch (NotImplementedException) { grafo = new GrafoMatriz(0); } //sin grafo todavía: solo quedan pendientes las pruebas que lo usan
            ClienteMensajeria mensajeria = new ClienteMensajeria("servidor-inexistente.invalid", "convitec", "convitec", codigoSede); //sin conexión: las otras sedes no responden
            Inventario = new ServicioInventario(sede, sedes, almacen);
            CoordinadorSedes coordinador = new CoordinadorSedes(codigoSede, mensajeria, Inventario);
            Inventario.Coordinador = coordinador;
            Pedidos = new ServicioPedidos(sede, grafo, Inventario, coordinador, almacen);
            Ventas = new ServicioVentas(almacen);
            Rutas = new ServicioRutas(sede, sedes, grafo, Pedidos, Ventas);
            Inventario.Cargar();
            Pedidos.Cargar();
            Ventas.Cargar();
        }

        public Sistema Reiniciar() { return new Sistema(_codigo, _config, _datos); } //otra instancia que lee los mismos archivos
    }

    private static SolicitudPedido Solicitud(string destino, string codigo, int cantidad) //pedido de una sola línea
    {
        return new SolicitudPedido { Destino = destino, Lineas = new[] { new LineaSolicitud { Codigo = codigo, Cantidad = cantidad } } };
    }

    private static string[] Codigos(ProductoLocal[] productos) //códigos de una lista de productos
    {
        string[] codigos = new string[productos.Length];
        for (int i = 0; i < productos.Length; i++) codigos[i] = productos[i].Codigo;
        return codigos;
    }

    private static string[] Ids(Pedido[] pedidos) //ids de una lista de pedidos
    {
        string[] ids = new string[pedidos.Length];
        for (int i = 0; i < pedidos.Length; i++) ids[i] = pedidos[i].Id;
        return ids;
    }
}
