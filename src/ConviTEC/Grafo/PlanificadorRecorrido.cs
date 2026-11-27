using ConviTEC.Estructuras; //ListaEnlazada
using ConviTEC.Modelos; //Pedido, EstadoRecorrido, Entrega, SiguienteEntrega, DestinoPendiente

namespace ConviTEC.Grafo;

public class PlanificadorRecorrido //recorrido voraz: en cada paso va al destino pendiente más cercano (Dijkstra)
{
    private GrafoMatriz _grafo; //red de rutas
    private string _origen; //ubicación de la sede (donde arranca cada recorrido)
    private bool _activo; //hay un recorrido iniciado
    private bool _finalizado; //ya no queda nada alcanzable por entregar
    private string _salida; //"11:00" o "15:00"
    private string _ubicacion; //dónde está el vehículo ahora
    private int _tiempoAcumulado; //minutos desde la salida
    private ListaEnlazada<Pedido> _pendientes = new ListaEnlazada<Pedido>(); //pedidos que faltan por entregar
    private ListaEnlazada<Entrega> _entregas = new ListaEnlazada<Entrega>(); //paradas ya realizadas, en orden

    public PlanificadorRecorrido(GrafoMatriz grafo, string origen) //origen = ubicación de la sede
    {
        _grafo = grafo;
        _origen = origen;
        _ubicacion = origen;
    }

    public bool Activo => _activo; //true mientras haya un recorrido iniciado
    public int TiempoAcumulado => _tiempoAcumulado; //minutos desde la salida (para Venta.TiempoTraslado)

    public EstadoRecorrido Iniciar(string salida, Pedido[] pendientes) //arranca en la sede con los pedidos dados
    {
        _activo = true; //se reinicia todo: el vehículo sale de la sede
        _salida = salida;
        _ubicacion = _origen;
        _tiempoAcumulado = 0;
        _pendientes = new ListaEnlazada<Pedido>();
        _entregas = new ListaEnlazada<Entrega>();
        if (pendientes != null)
            for (int i = 0; i < pendientes.Length; i++)
                if (pendientes[i] != null) _pendientes.Agregar(pendientes[i]);

        _finalizado = Siguiente(Dijkstra.Calcular(_grafo, _ubicacion)) == null; //sin pedidos alcanzables termina de una vez
        return Estado();
    }

    public EstadoRecorrido Estado() //estado del recorrido en curso (Activo = false si no hay)
    {
        if (!_activo)
            return new EstadoRecorrido { Activo = false, Origen = _origen, UbicacionActual = _origen, Entregas = new Entrega[0], Pendientes = new DestinoPendiente[0] };

        ResultadoDijkstra r = Dijkstra.Calcular(_grafo, _ubicacion); //tiempos desde donde está el vehículo
        return new EstadoRecorrido
        {
            Activo = true,
            Finalizado = _finalizado,
            Salida = _salida,
            Origen = _origen,
            UbicacionActual = _ubicacion,
            TiempoTotal = _tiempoAcumulado,
            Entregas = _entregas.ComoArreglo(),
            Siguiente = Siguiente(r),
            Pendientes = Pendientes(r)
        };
    }

    public Pedido[] EntregarSiguiente() //viaja al destino más cercano y devuelve los pedidos entregados ahí; vacío si no queda nada alcanzable
    {
        if (!_activo || _finalizado) return new Pedido[0];

        ResultadoDijkstra r = Dijkstra.Calcular(_grafo, _ubicacion); //se recalcula desde la ubicación actual
        SiguienteEntrega siguiente = Siguiente(r);
        if (siguiente == null) { _finalizado = true; return new Pedido[0]; }

        ListaEnlazada<Pedido> entregados = new ListaEnlazada<Pedido>(); //todos los pedidos de ese destino salen en esta visita
        int i = 0;
        while (i < _pendientes.Cantidad)
        {
            Pedido p = _pendientes.Obtener(i);
            if (p.Destino == siguiente.Destino) { entregados.Agregar(p); _pendientes.EliminarEn(i); } //no se avanza i: el siguiente ocupó su lugar
            else i++;
        }

        _tiempoAcumulado += siguiente.Tiempo;
        _entregas.Agregar(new Entrega
        {
            Orden = _entregas.Cantidad + 1,
            Destino = siguiente.Destino,
            Pedidos = siguiente.Pedidos,
            Tramo = siguiente.Tiempo,
            Acumulado = _tiempoAcumulado,
            Camino = siguiente.Camino
        });
        _ubicacion = siguiente.Destino; //el vehículo queda en el destino entregado

        _finalizado = Siguiente(Dijkstra.Calcular(_grafo, _ubicacion)) == null; //¿queda algo alcanzable desde aquí?
        return entregados.ComoArreglo();
    }

    // Auxiliares privados
    private SiguienteEntrega Siguiente(ResultadoDijkstra r) //destino pendiente alcanzable con menor tiempo; null si no hay
    {
        Pedido mejor = null;
        int mejorTiempo = -1;
        for (int i = 0; i < _pendientes.Cantidad; i++)
        {
            Pedido p = _pendientes.Obtener(i);
            int tiempo = r.TiempoHasta(p.Destino); //-1 si es inalcanzable
            if (tiempo != -1 && (mejor == null || tiempo < mejorTiempo)) { mejor = p; mejorTiempo = tiempo; } //en empate gana el pedido más antiguo
        }
        if (mejor == null) return null;
        return new SiguienteEntrega { Destino = mejor.Destino, Tiempo = mejorTiempo, Camino = r.CaminoHasta(mejor.Destino), Pedidos = IdsHacia(mejor.Destino) };
    }

    private DestinoPendiente[] Pendientes(ResultadoDijkstra r) //un registro por destino (agrupa los pedidos al mismo lugar)
    {
        ListaEnlazada<string> destinos = new ListaEnlazada<string>(); //destinos distintos, en orden de llegada
        for (int i = 0; i < _pendientes.Cantidad; i++)
        {
            string destino = _pendientes.Obtener(i).Destino;
            if (destinos.IndiceDe(d => d == destino) == -1) destinos.Agregar(destino);
        }

        DestinoPendiente[] resultado = new DestinoPendiente[destinos.Cantidad];
        for (int i = 0; i < resultado.Length; i++)
        {
            string destino = destinos.Obtener(i);
            int tiempo = r.TiempoHasta(destino);
            resultado[i] = new DestinoPendiente { Destino = destino, Pedidos = IdsHacia(destino), Tiempo = tiempo == -1 ? null : tiempo }; //null = inalcanzable
        }
        return resultado;
    }

    private string[] IdsHacia(string destino) //ids de los pedidos pendientes hacia ese destino
    {
        int cuantos = 0; //primero se cuentan, para crear el arreglo del tamaño justo
        for (int i = 0; i < _pendientes.Cantidad; i++)
            if (_pendientes.Obtener(i).Destino == destino) cuantos++;

        string[] ids = new string[cuantos];
        int k = 0;
        for (int i = 0; i < _pendientes.Cantidad; i++)
            if (_pendientes.Obtener(i).Destino == destino) ids[k++] = _pendientes.Obtener(i).Id;
        return ids;
    }
}