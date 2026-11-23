using System; //ArgumentException
using System.IO; //Path
using ConviTEC.Grafo; //GrafoMatriz, CargadorGrafo, Dijkstra, ResultadoDijkstra, PlanificadorRecorrido
using ConviTEC.Modelos; //Pedido, EstadoRecorrido, GrafoRespuesta
using ConviTEC.Servicios; //ServicioRutas
using static ConviTEC.Pruebas.EjecutorPruebas; //Prueba(...)

namespace ConviTEC.Pruebas;

public static class PruebasRolB //Palma: grafo, Dijkstra y recorrido de entregas
{
    public static void Ejecutar(string carpetaConfig) //registra y ejecuta todas las pruebas del rol
    {
        string rutas = Path.Combine(carpetaConfig, "rutas.json"); //las 15 rutas de la tabla del enunciado

        // GrafoMatriz
        Prueba("GrafoMatriz: nodos y aristas en ambos sentidos", () =>
        {
            GrafoMatriz grafo = new GrafoMatriz(4);
            Verificar.Igual(0, grafo.AgregarNodo("Alajuela"), "índice del primer nodo");
            Verificar.Igual(1, grafo.AgregarNodo("Heredia"), "índice del segundo nodo");
            Verificar.Igual(0, grafo.AgregarNodo("Alajuela"), "agregar un nodo repetido devuelve su índice");
            Verificar.Igual(2, grafo.CantidadNodos, "CantidadNodos");
            Verificar.Verdadero(grafo.AgregarArista("Alajuela", "Heredia", 20), "AgregarArista");
            Verificar.Igual(20, grafo.Peso(0, 1), "peso Alajuela → Heredia");
            Verificar.Igual(20, grafo.Peso(1, 0), "peso Heredia → Alajuela (no dirigido)");
            Verificar.Igual(1, grafo.Aristas().Length, "Aristas debe contar cada ruta una sola vez");
        });

        Prueba("GrafoMatriz: casos inválidos", () =>
        {
            GrafoMatriz grafo = new GrafoMatriz(4);
            grafo.AgregarNodo("Alajuela");
            grafo.AgregarNodo("Heredia");
            Verificar.Igual(false, grafo.AgregarArista("Alajuela", "Marte", 10), "arista con un nodo inexistente");
            Verificar.Igual(false, grafo.AgregarArista("Alajuela", "Heredia", 0), "arista con tiempo 0");
            Verificar.Igual(-1, grafo.IndiceDe("Marte"), "IndiceDe de un nodo inexistente");
            Verificar.Igual(GrafoMatriz.SinArista, grafo.Peso(0, 1), "peso sin arista");
        });

        // CargadorGrafo
        Prueba("CargadorGrafo: carga config/rutas.json", () =>
        {
            GrafoMatriz grafo = CargadorGrafo.Cargar(rutas);
            Verificar.Igual(13, grafo.CantidadNodos, "CantidadNodos");
            Verificar.Igual(15, grafo.Aristas().Length, "cantidad de aristas");
            Verificar.Igual(20, grafo.Peso(grafo.IndiceDe("Alajuela"), grafo.IndiceDe("Heredia")), "peso Alajuela – Heredia");
            Verificar.Verdadero(grafo.IndiceDe("San José") != -1, "\"San José\" (con tilde) debe existir");
        });

        Prueba("CargadorGrafo: archivo inexistente lanza excepción", () =>
        {
            Verificar.Lanza<Exception>(() => CargadorGrafo.Cargar(Path.Combine(carpetaConfig, "no-existe.json")), "Cargar un archivo inexistente");
        });

        // Dijkstra
        Prueba("Dijkstra: tiempos y caminos mínimos desde Alajuela", () =>
        {
            ResultadoDijkstra r = Dijkstra.Calcular(CargadorGrafo.Cargar(rutas), "Alajuela");
            Verificar.Igual(0, r.TiempoHasta("Alajuela"), "tiempo al origen");
            Verificar.Igual(20, r.TiempoHasta("Heredia"), "Alajuela → Heredia");
            Verificar.Igual(80, r.TiempoHasta("Cartago"), "Alajuela → Cartago (por San José, no por Heredia)");
            Verificar.Igual(215, r.TiempoHasta("Limón"), "Alajuela → Limón");
            Verificar.Arreglo(new[] { "Alajuela", "San José", "Cartago", "Paraíso", "Turrialba", "Limón" }, r.CaminoHasta("Limón"), "camino Alajuela → Limón");
        });

        Prueba("Dijkstra: Guanacaste es un componente aislado", () =>
        {
            ResultadoDijkstra r = Dijkstra.Calcular(CargadorGrafo.Cargar(rutas), "Liberia");
            Verificar.Igual(65, r.TiempoHasta("Cañas"), "Liberia → Cañas");
            Verificar.Igual(false, r.EsAlcanzable("Cartago"), "Cartago no es alcanzable desde Liberia");
            Verificar.Igual(-1, r.TiempoHasta("Cartago"), "TiempoHasta de un destino inalcanzable");
            Verificar.Igual(0, r.CaminoHasta("Cartago").Length, "CaminoHasta de un destino inalcanzable");
            Verificar.Igual(false, r.EsAlcanzable("Marte"), "EsAlcanzable de un nodo inexistente");
            for (int i = 0; i < r.Distancias.Length; i++) //int.MaxValue + algo da un número negativo
                Verificar.Verdadero(r.Distancias[i] >= 0, "ninguna distancia puede ser negativa (revisar SinArista antes de sumar)");
        });

        Prueba("Dijkstra: origen inexistente lanza ArgumentException", () =>
        {
            Verificar.Lanza<ArgumentException>(() => Dijkstra.Calcular(CargadorGrafo.Cargar(rutas), "Marte"), "Calcular desde Marte");
        });

        // PlanificadorRecorrido
        Prueba("PlanificadorRecorrido: ejemplo Heredia → Cartago → Puntarenas = 245 min", () =>
        {
            PlanificadorRecorrido plan = new PlanificadorRecorrido(CargadorGrafo.Cargar(rutas), "Alajuela");
            Pedido[] pedidos = { PedidoA("ALA-0001", "Cartago"), PedidoA("ALA-0002", "Heredia"), PedidoA("ALA-0003", "Puntarenas"), PedidoA("ALA-0004", "Heredia") };
            EstadoRecorrido estado = plan.Iniciar("11:00", pedidos);
            Verificar.Verdadero(estado.Activo, "el recorrido debe quedar activo");
            Verificar.Igual("Heredia", estado.Siguiente.Destino, "primer destino (el más cercano)");
            Verificar.Igual(20, estado.Siguiente.Tiempo, "tiempo al primer destino");

            Verificar.Igual(2, plan.EntregarSiguiente().Length, "en Heredia se entregan los 2 pedidos de ese destino");
            Verificar.Igual(20, plan.TiempoAcumulado, "acumulado tras Heredia");
            Verificar.Igual(1, plan.EntregarSiguiente().Length, "entrega en Cartago");
            Verificar.Igual(85, plan.TiempoAcumulado, "acumulado tras Cartago (20 + 65)");
            Verificar.Igual(1, plan.EntregarSiguiente().Length, "entrega en Puntarenas");
            Verificar.Igual(245, plan.TiempoAcumulado, "tiempo total (85 + 160)");
            Verificar.Igual(0, plan.EntregarSiguiente().Length, "ya no queda nada por entregar");

            EstadoRecorrido final = plan.Estado();
            Verificar.Igual(3, final.Entregas.Length, "cantidad de paradas");
            Verificar.Igual("Puntarenas", final.UbicacionActual, "ubicación final");
            Verificar.Igual(245, final.TiempoTotal, "TiempoTotal");
        });

        Prueba("PlanificadorRecorrido: los destinos inalcanzables quedan pendientes", () =>
        {
            PlanificadorRecorrido plan = new PlanificadorRecorrido(CargadorGrafo.Cargar(rutas), "Liberia");
            plan.Iniciar("15:00", new[] { PedidoA("LIB-0001", "Cartago"), PedidoA("LIB-0002", "Cañas") });
            Verificar.Igual(1, plan.EntregarSiguiente().Length, "entrega en Cañas");
            Verificar.Igual(0, plan.EntregarSiguiente().Length, "Cartago no se puede entregar");
            EstadoRecorrido estado = plan.Estado();
            Verificar.Nulo(estado.Siguiente, "Siguiente debe ser null");
            Verificar.Igual(1, estado.Pendientes.Length, "Cartago debe quedar en Pendientes");
            Verificar.Igual("Cartago", estado.Pendientes[0].Destino, "destino pendiente");
            Verificar.Nulo(estado.Pendientes[0].Tiempo, "Tiempo = null por ser inalcanzable");
        });

        // ServicioRutas (solo lo que no necesita pedidos ni ventas)
        Prueba("ServicioRutas: destinos y grafo para la web", () =>
        {
            ServicioRutas servicio = new ServicioRutas(DatosPrueba.Sedes()[1], DatosPrueba.Sedes(), CargadorGrafo.Cargar(rutas), null, null);
            string[] destinos = servicio.Destinos();
            Verificar.Igual(12, destinos.Length, "todos los nodos menos la sede");
            for (int i = 0; i < destinos.Length; i++)
                Verificar.Verdadero(destinos[i] != "Alajuela", "la ubicación de la sede no debe ser un destino");

            GrafoRespuesta grafo = servicio.Grafo();
            Verificar.Igual(13, grafo.Nodos.Length, "nodos del grafo");
            Verificar.Igual(15, grafo.Aristas.Length, "aristas del grafo");
            for (int i = 0; i < grafo.Nodos.Length; i++)
            {
                if (grafo.Nodos[i].Nombre == "San José") Verificar.Igual("SJO", grafo.Nodos[i].Sede, "San José es la sede SJO");
                if (grafo.Nodos[i].Nombre == "Heredia") Verificar.Nulo(grafo.Nodos[i].Sede, "Heredia no es sede");
            }
        });
    }

    private static Pedido PedidoA(string id, string destino) //pedido pendiente hacia un destino
    {
        return new Pedido { Id = id, Destino = destino, Estado = EstadoPedido.Pendiente, Lineas = new LineaPedido[0], SedesParticipantes = new string[0] };
    }
}
