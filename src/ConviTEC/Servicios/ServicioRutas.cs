using System; //ArgumentException, InvalidOperationException
using ConviTEC.Estructuras; //Ordenamiento
using ConviTEC.Grafo; //GrafoMatriz, PlanificadorRecorrido
using ConviTEC.Modelos; //Sede, Pedido, GrafoRespuesta, NodoGrafo, EstadoRecorrido

namespace ConviTEC.Servicios;

public class ServicioRutas //grafo para la web y recorrido de entregas de la sede
{
    private Sede _sede; //sede de esta instancia (su ubicación es el origen de los recorridos)
    private Sede[] _sedes; //todas las sedes, para marcar sus nodos en el grafo
    private GrafoMatriz _grafo; //red de rutas
    private ServicioPedidos _pedidos; //de aquí salen los pendientes y aquí se marcan los entregados
    private ServicioVentas _ventas; //cada entrega registra una venta
    private PlanificadorRecorrido _planificador; //recorrido en curso de la sede

    public ServicioRutas(Sede sede, Sede[] sedes, GrafoMatriz grafo, ServicioPedidos pedidos, ServicioVentas ventas) //solo guarda las dependencias
    {
        _sede = sede;
        _sedes = sedes ?? new Sede[0];
        _grafo = grafo ?? new GrafoMatriz(0);
        _pedidos = pedidos;
        _ventas = ventas;
        _planificador = new PlanificadorRecorrido(_grafo, sede == null ? null : sede.Ubicacion);
    }

    public GrafoRespuesta Grafo() //nodos (marcando cuáles son sedes) y aristas
    {
        lock (Sincronizacion.Candado)
        {
            string[] nombres = _grafo.Nombres();
            NodoGrafo[] nodos = new NodoGrafo[nombres.Length];
            for (int i = 0; i < nombres.Length; i++)
                nodos[i] = new NodoGrafo { Nombre = nombres[i], Sede = SedeEn(nombres[i]) };
            return new GrafoRespuesta { Nodos = nodos, Aristas = _grafo.Aristas() };
        }
    }

    public string[] Destinos() //nodos que se pueden elegir como destino (todos menos la sede), en orden alfabético
    {
        lock (Sincronizacion.Candado)
        {
            string[] nombres = _grafo.Nombres();
            int cuantos = 0; //primero se cuentan, para crear el arreglo del tamaño justo
            for (int i = 0; i < nombres.Length; i++)
                if (nombres[i] != _sede.Ubicacion) cuantos++;

            string[] destinos = new string[cuantos];
            int k = 0;
            for (int i = 0; i < nombres.Length; i++)
                if (nombres[i] != _sede.Ubicacion) destinos[k++] = nombres[i];

            Ordenamiento.Ordenar(destinos, (a, b) => string.Compare(a, b, StringComparison.CurrentCulture)); //merge sort propio; la web no ordena
            return destinos;
        }
    }

    public EstadoRecorrido Estado() //recorrido en curso, o pendientes con su tiempo desde la sede si no hay recorrido
    {
        lock (Sincronizacion.Candado)
        {
            if (_planificador.Activo) return _planificador.Estado();

            PlanificadorRecorrido vistaPrevia = new PlanificadorRecorrido(_grafo, _sede.Ubicacion); //planificador temporal: agrupa los pendientes y mide desde la sede
            EstadoRecorrido estado = vistaPrevia.Iniciar(null, PedidosPendientes());
            estado.Activo = false; //no hay recorrido real: solo se muestran los pendientes
            estado.Finalizado = false;
            return estado;
        }
    }

    public EstadoRecorrido Iniciar(string salida) //ArgumentException si la salida no es "11:00" ni "15:00"
    {
        lock (Sincronizacion.Candado)
        {
            salida = salida == null ? null : salida.Trim();
            if (salida != "11:00" && salida != "15:00") throw new ArgumentException("La salida debe ser a las 11:00 o a las 15:00.");
            if (_planificador.Activo && !_planificador.Estado().Finalizado) throw new InvalidOperationException("Ya hay un recorrido en curso: termine las entregas antes de iniciar otro.");

            Pedido[] pendientes = PedidosPendientes();
            if (pendientes.Length == 0) throw new InvalidOperationException("No hay pedidos pendientes de entrega.");
            return _planificador.Iniciar(salida, pendientes);
        }
    }

    public EstadoRecorrido EntregarSiguiente() //entrega, registra ventas y marca entregados; InvalidOperationException si no hay recorrido activo
    {
        lock (Sincronizacion.Candado)
        {
            if (!_planificador.Activo) throw new InvalidOperationException("No hay un recorrido activo. Inicie una salida primero.");

            Pedido[] entregados = _planificador.EntregarSiguiente(); //Dijkstra desde la ubicación actual → destino más cercano
            if (entregados.Length == 0) throw new InvalidOperationException("El recorrido terminó: no quedan destinos alcanzables.");

            for (int i = 0; i < entregados.Length; i++)
                _ventas.RegistrarEntrega(entregados[i], _planificador.TiempoAcumulado); //tiempo acumulado hasta esta parada
            _pedidos.MarcarEntregados(entregados);
            return _planificador.Estado();
        }
    }

    // Auxiliares privados
    private string SedeEn(string nodo) //código de la sede ubicada en ese nodo; null si no hay sede
    {
        for (int i = 0; i < _sedes.Length; i++)
            if (_sedes[i].Ubicacion == nodo) return _sedes[i].Codigo;
        return null;
    }

    private Pedido[] PedidosPendientes() => _pedidos == null ? new Pedido[0] : _pedidos.Pendientes(); //vacío si no hay servicio de pedidos
}