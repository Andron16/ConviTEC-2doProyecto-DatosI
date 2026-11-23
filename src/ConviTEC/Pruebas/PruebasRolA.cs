using System; //ArgumentOutOfRangeException, InvalidOperationException, DateTime
using ConviTEC.Estructuras; //ListaEnlazada, Cola, ArbolAVL, Ordenamiento
using ConviTEC.Modelos; //Existencia, Pedido, LineaPedido, EstadoPedido
using ConviTEC.Persistencia; //AlmacenSede
using ConviTEC.Servicios; //ServicioVentas
using static ConviTEC.Pruebas.EjecutorPruebas; //Prueba(...)

namespace ConviTEC.Pruebas;

public static class PruebasRolA //Celeste: estructuras de datos, persistencia y ventas
{
    public static void Ejecutar() //registra y ejecuta todas las pruebas del rol
    {
        // ListaEnlazada
        Prueba("ListaEnlazada: agregar al final y al inicio", () =>
        {
            ListaEnlazada<int> lista = new ListaEnlazada<int>();
            Verificar.Verdadero(lista.EstaVacia, "una lista nueva debe estar vacía");
            lista.Agregar(2);
            lista.Agregar(3);
            lista.AgregarAlInicio(1);
            Verificar.Igual(3, lista.Cantidad, "Cantidad");
            Verificar.Arreglo(new[] { 1, 2, 3 }, lista.ComoArreglo(), "ComoArreglo");
            Verificar.Igual(2, lista.Obtener(1), "Obtener(1)");
        });

        Prueba("ListaEnlazada: índices inválidos", () =>
        {
            ListaEnlazada<int> lista = new ListaEnlazada<int>();
            lista.Agregar(5);
            Verificar.Lanza<ArgumentOutOfRangeException>(() => lista.Obtener(1), "Obtener(1) con un solo elemento");
            Verificar.Lanza<ArgumentOutOfRangeException>(() => lista.Obtener(-1), "Obtener(-1)");
            Verificar.Igual(false, lista.EliminarEn(3), "EliminarEn(3) debe devolver false");
        });

        Prueba("ListaEnlazada: eliminar al inicio, al final y en medio", () =>
        {
            ListaEnlazada<int> lista = new ListaEnlazada<int>();
            for (int i = 1; i <= 5; i++) lista.Agregar(i);
            lista.EliminarEn(0); //[2,3,4,5]
            lista.EliminarEn(3); //[2,3,4]
            lista.EliminarEn(1); //[2,4]
            lista.Agregar(9);    //el final debe seguir bien enlazado después de borrar el último
            Verificar.Arreglo(new[] { 2, 4, 9 }, lista.ComoArreglo(), "ComoArreglo");
            Verificar.Igual(3, lista.Cantidad, "Cantidad");
        });

        Prueba("ListaEnlazada: Buscar e IndiceDe", () =>
        {
            ListaEnlazada<string> lista = new ListaEnlazada<string>();
            lista.Agregar("ALA-0001");
            lista.Agregar("SJO-0001");
            Verificar.Igual("SJO-0001", lista.Buscar(x => x.StartsWith("SJO")), "Buscar");
            Verificar.Igual(1, lista.IndiceDe(x => x == "SJO-0001"), "IndiceDe existente");
            Verificar.Nulo(lista.Buscar(x => x == "LIM-0001"), "Buscar sin coincidencias");
            Verificar.Igual(-1, lista.IndiceDe(x => x == "LIM-0001"), "IndiceDe sin coincidencias");
        });

        // Cola
        Prueba("Cola: orden FIFO", () =>
        {
            Cola<int> cola = new Cola<int>();
            cola.Encolar(1);
            cola.Encolar(2);
            cola.Encolar(3);
            Verificar.Igual(1, cola.VerFrente(), "VerFrente");
            Verificar.Igual(1, cola.Desencolar(), "primer Desencolar");
            Verificar.Igual(2, cola.Desencolar(), "segundo Desencolar");
            cola.Encolar(4);
            Verificar.Arreglo(new[] { 3, 4 }, cola.ComoArreglo(), "ComoArreglo");
            Verificar.Igual(2, cola.Cantidad, "Cantidad");
        });

        Prueba("Cola: vacía lanza InvalidOperationException y se puede reutilizar", () =>
        {
            Cola<int> cola = new Cola<int>();
            Verificar.Lanza<InvalidOperationException>(() => cola.Desencolar(), "Desencolar en cola vacía");
            Verificar.Lanza<InvalidOperationException>(() => cola.VerFrente(), "VerFrente en cola vacía");
            cola.Encolar(1);
            cola.Desencolar();
            cola.Encolar(2); //el final debe reiniciarse al vaciarse
            Verificar.Igual(2, cola.VerFrente(), "VerFrente después de vaciar y volver a encolar");
            Verificar.Verdadero(!cola.EstaVacia, "la cola no debe estar vacía");
        });

        // ArbolAVL
        Prueba("ArbolAVL: insertar, buscar y duplicados", () =>
        {
            ArbolAVL<string, string> arbol = new ArbolAVL<string, string>();
            Verificar.Verdadero(arbol.Insertar("C001", "Café"), "Insertar C001");
            arbol.Insertar("A003", "Agua");
            arbol.Insertar("G014", "Galletas");
            Verificar.Igual(false, arbol.Insertar("A003", "Otro"), "Insertar una clave repetida debe devolver false");
            Verificar.Igual(3, arbol.Cantidad, "Cantidad");
            Verificar.Igual("Agua", arbol.Buscar("A003"), "Buscar A003 (no debe cambiar con el duplicado)");
            Verificar.Nulo(arbol.Buscar("Z999"), "Buscar una clave inexistente");
            Verificar.Verdadero(arbol.Contiene("G014") && !arbol.Contiene("Z999"), "Contiene");
            Verificar.Arreglo(new[] { "Agua", "Café", "Galletas" }, arbol.EnOrden(), "EnOrden (ordenado por clave)");
        });

        Prueba("ArbolAVL: las 4 rotaciones", () =>
        {
            int[][] casos = { new[] { 3, 2, 1 }, new[] { 1, 2, 3 }, new[] { 3, 1, 2 }, new[] { 1, 3, 2 } }; //II, DD, ID, DI
            string[] nombres = { "simple a la derecha (3,2,1)", "simple a la izquierda (1,2,3)", "doble izquierda-derecha (3,1,2)", "doble derecha-izquierda (1,3,2)" };
            for (int c = 0; c < casos.Length; c++)
            {
                ArbolAVL<int, int> arbol = new ArbolAVL<int, int>();
                for (int i = 0; i < 3; i++) arbol.Insertar(casos[c][i], casos[c][i]);
                Verificar.Igual(2, arbol.Altura(), "altura tras rotación " + nombres[c]);
                Verificar.Arreglo(new[] { 1, 2, 3 }, arbol.EnOrden(), "EnOrden tras rotación " + nombres[c]);
            }
        });

        Prueba("ArbolAVL: se mantiene balanceado con 100 claves en orden", () =>
        {
            ArbolAVL<int, int> arbol = new ArbolAVL<int, int>();
            for (int i = 1; i <= 100; i++) arbol.Insertar(i, i);
            Verificar.Igual(100, arbol.Cantidad, "Cantidad");
            Verificar.Verdadero(arbol.Altura() <= 9, $"la altura debe ser como máximo 9 y es {arbol.Altura()} (un ABB sin balancear tendría 100)");
            int[] enOrden = arbol.EnOrden();
            for (int i = 0; i < 100; i++) Verificar.Igual(i + 1, enOrden[i], $"EnOrden[{i}]");
        });

        Prueba("ArbolAVL: eliminar", () =>
        {
            ArbolAVL<int, int> arbol = new ArbolAVL<int, int>();
            for (int i = 1; i <= 7; i++) arbol.Insertar(i, i);
            Verificar.Igual(3, arbol.Altura(), "altura con 7 claves");
            Verificar.Verdadero(arbol.Eliminar(4), "Eliminar 4 (nodo con dos hijos)");
            Verificar.Igual(false, arbol.Eliminar(99), "Eliminar una clave inexistente debe devolver false");
            Verificar.Arreglo(new[] { 1, 2, 3, 5, 6, 7 }, arbol.EnOrden(), "EnOrden tras eliminar 4");
            arbol.Eliminar(1);
            arbol.Eliminar(2);
            arbol.Eliminar(3);
            Verificar.Igual(3, arbol.Cantidad, "Cantidad");
            Verificar.Igual(2, arbol.Altura(), "altura tras eliminar (debe rebalancear)");
        });

        Prueba("ArbolAVL: árbol vacío", () =>
        {
            ArbolAVL<string, string> arbol = new ArbolAVL<string, string>();
            Verificar.Igual(0, arbol.Altura(), "Altura");
            Verificar.Igual(0, arbol.Cantidad, "Cantidad");
            Verificar.Igual(0, arbol.EnOrden().Length, "EnOrden debe ser un arreglo vacío");
            Verificar.Nulo(arbol.Buscar("C001"), "Buscar");
            Verificar.Igual(false, arbol.Eliminar("C001"), "Eliminar");
        });

        // Ordenamiento
        Prueba("Ordenamiento: ordena números de menor a mayor y de mayor a menor", () =>
        {
            int[] datos = { 5, 1, 4, 2, 3, 1 };
            Ordenamiento.Ordenar(datos, (a, b) => a.CompareTo(b));
            Verificar.Arreglo(new[] { 1, 1, 2, 3, 4, 5 }, datos, "ascendente");
            Ordenamiento.Ordenar(datos, (a, b) => b.CompareTo(a));
            Verificar.Arreglo(new[] { 5, 4, 3, 2, 1, 1 }, datos, "descendente");
        });

        Prueba("Ordenamiento: es estable y acepta arreglos vacíos", () =>
        {
            string[] datos = { "b1", "a1", "b2", "a2" };
            Ordenamiento.Ordenar(datos, (x, y) => x[0].CompareTo(y[0])); //compara solo la letra
            Verificar.Arreglo(new[] { "a1", "a2", "b1", "b2" }, datos, "los iguales conservan su orden original");
            int[] vacio = new int[0];
            Ordenamiento.Ordenar(vacio, (a, b) => a.CompareTo(b));
            Verificar.Igual(0, vacio.Length, "arreglo vacío");
        });

        // AlmacenSede
        Prueba("AlmacenSede: lee la configuración compartida", () =>
        {
            AlmacenSede almacen = new AlmacenSede(DatosPrueba.CrearConfig(), DatosPrueba.CarpetaTemporal(), "ALA");
            Verificar.Igual(4, almacen.CargarSedes().Length, "cantidad de sedes");
            Verificar.Igual("San José", almacen.CargarSedes()[2].Nombre, "nombre de la tercera sede");
            Verificar.Igual(3, almacen.CargarProductos().Length, "cantidad de productos");
        });

        Prueba("AlmacenSede: la primera vez arma el inventario con productos e inventario inicial", () =>
        {
            AlmacenSede almacen = new AlmacenSede(DatosPrueba.CrearConfig(), DatosPrueba.CarpetaTemporal(), "ALA");
            Existencia[] inventario = almacen.CargarInventario();
            Verificar.Igual(3, inventario.Length, "cantidad de productos en el inventario");
            Existencia cafe = null;
            for (int i = 0; i < inventario.Length; i++)
                if (inventario[i].Producto.Codigo == "C001") cafe = inventario[i];
            Verificar.NoNulo(cafe, "existencia de C001");
            Verificar.Igual("Café molido 250 g", cafe.Producto.Nombre, "nombre de C001");
            Verificar.Igual(10, cafe.Disponible, "disponible inicial de C001 en ALA");
            Verificar.Igual(0, cafe.Reservado, "reservado inicial");
        });

        Prueba("AlmacenSede: archivos que no existen devuelven arreglos vacíos", () =>
        {
            AlmacenSede almacen = new AlmacenSede(DatosPrueba.CrearConfig(), DatosPrueba.CarpetaTemporal(), "ALA");
            Verificar.Igual(0, almacen.CargarPedidos().Length, "CargarPedidos");
            Verificar.Igual(0, almacen.CargarVentas().Length, "CargarVentas");
            Verificar.Igual(0, almacen.CargarReservas().Length, "CargarReservas");
            Verificar.Igual(0, almacen.CargarCopiaGlobal().Length, "CargarCopiaGlobal");
        });

        Prueba("AlmacenSede: guardar y volver a cargar (como al reiniciar la sede)", () =>
        {
            string config = DatosPrueba.CrearConfig();
            string datos = DatosPrueba.CarpetaTemporal();
            AlmacenSede almacen = new AlmacenSede(config, datos, "ALA");
            Existencia[] inventario = almacen.CargarInventario();
            inventario[0].Disponible = 99;
            almacen.GuardarInventario(inventario);
            almacen.GuardarPedidos(new[] { new Pedido { Id = "ALA-0001", Estado = EstadoPedido.Pendiente, Destino = "Heredia", Lineas = new[] { new LineaPedido { Codigo = "C001", Cantidad = 2, PrecioUnitario = 2800 } }, Total = 5600 } });

            AlmacenSede otro = new AlmacenSede(config, datos, "ALA"); //otra instancia lee lo que guardó la primera
            Verificar.Igual(99, otro.CargarInventario()[0].Disponible, "disponible guardado");
            Pedido[] pedidos = otro.CargarPedidos();
            Verificar.Igual(1, pedidos.Length, "cantidad de pedidos");
            Verificar.Igual("ALA-0001", pedidos[0].Id, "Id");
            Verificar.Igual(EstadoPedido.Pendiente, pedidos[0].Estado, "Estado");
            Verificar.Igual("C001", pedidos[0].Lineas[0].Codigo, "código de la línea");
        });

        // ServicioVentas
        Prueba("ServicioVentas: historial y estadísticas", () =>
        {
            AlmacenSede almacen = new AlmacenSede(DatosPrueba.CrearConfig(), DatosPrueba.CarpetaTemporal(), "ALA");
            ServicioVentas ventas = new ServicioVentas(almacen);
            ventas.Cargar();
            ventas.RegistrarEntrega(PedidoDePrueba("ALA-0001", "C001", 2, 2800), 20);  //5600 / 20  = 280
            ventas.RegistrarEntrega(PedidoDePrueba("ALA-0002", "G014", 10, 950), 85); //9500 / 85  = 111.8
            ventas.RegistrarEntrega(PedidoDePrueba("ALA-0003", "A003", 1, 600), 245); //600  / 245 = 2.4

            Verificar.Igual("ALA-0003", ventas.Historial()[0].PedidoId, "el historial empieza por la venta más reciente");
            Verificar.Igual(245, ventas.Historial()[0].TiempoTraslado, "TiempoTraslado");
            Estadisticas e = ventas.Calcular();
            Verificar.Igual(15700, e.IngresosTotales, "IngresosTotales");
            Verificar.Igual(3, e.PedidosEntregados, "PedidosEntregados");
            Verificar.Igual("G014", e.ProductoEstrella.Codigo, "producto estrella (más ingresos)");
            Verificar.Igual(9500, e.ProductoEstrella.Ingresos, "ingresos del producto estrella");
            string[] peores = new string[e.PeoresPedidos.Length];
            for (int i = 0; i < peores.Length; i++) peores[i] = e.PeoresPedidos[i].Id;
            Verificar.Arreglo(new[] { "ALA-0003", "ALA-0002", "ALA-0001" }, peores, "peores pedidos de menor a mayor rendimiento");

            ServicioVentas recargadas = new ServicioVentas(almacen); //como al reiniciar la sede
            recargadas.Cargar();
            Verificar.Igual(3, recargadas.Historial().Length, "ventas recuperadas del archivo");
        });

        Prueba("ServicioVentas: sin ventas y máximo 5 peores pedidos", () =>
        {
            ServicioVentas ventas = new ServicioVentas(new AlmacenSede(DatosPrueba.CrearConfig(), DatosPrueba.CarpetaTemporal(), "ALA"));
            ventas.Cargar();
            Estadisticas vacia = ventas.Calcular();
            Verificar.Igual(0, vacia.IngresosTotales, "IngresosTotales sin ventas");
            Verificar.Nulo(vacia.ProductoEstrella, "ProductoEstrella sin ventas");
            Verificar.Igual(0, vacia.PeoresPedidos.Length, "PeoresPedidos sin ventas");
            for (int i = 1; i <= 7; i++) ventas.RegistrarEntrega(PedidoDePrueba("ALA-000" + i, "C001", 1, 2800), i * 10);
            Verificar.Igual(5, ventas.Calcular().PeoresPedidos.Length, "PeoresPedidos debe tener como máximo 5");
        });
    }

    private static Pedido PedidoDePrueba(string id, string codigo, int cantidad, int precio) //pedido de una sola línea
    {
        return new Pedido
        {
            Id = id,
            Fecha = DateTime.Now,
            Estado = EstadoPedido.Pendiente,
            Destino = "Heredia",
            Lineas = new[] { new LineaPedido { Codigo = codigo, Nombre = codigo, Cantidad = cantidad, PrecioUnitario = precio } },
            Total = cantidad * precio,
            SedesParticipantes = new[] { "ALA" }
        };
    }
}
