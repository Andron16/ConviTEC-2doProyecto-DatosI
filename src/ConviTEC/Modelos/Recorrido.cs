namespace ConviTEC.Modelos;

public class Entrega //parada ya realizada en el recorrido
{
    public int Orden { get; set; } //1, 2, 3...
    public string Destino { get; set; }
    public string[] Pedidos { get; set; } //ids entregados en esa parada
    public int Tramo { get; set; } //minutos desde la parada anterior
    public int Acumulado { get; set; } //minutos desde la salida de la sede
    public string[] Camino { get; set; } //nodos recorridos en el tramo
}

public class SiguienteEntrega //destino pendiente más cercano según Dijkstra
{
    public string Destino { get; set; }
    public int Tiempo { get; set; }
    public string[] Camino { get; set; }
    public string[] Pedidos { get; set; }
}

public class DestinoPendiente //destino que falta por visitar
{
    public string Destino { get; set; }
    public string[] Pedidos { get; set; }
    public int? Tiempo { get; set; } //minutos desde la ubicación actual; null = inalcanzable
}

public class EstadoRecorrido //lo que devuelve /api/recorrido
{
    public bool Activo { get; set; }
    public bool Finalizado { get; set; }
    public string Salida { get; set; } //"11:00" o "15:00"
    public string Origen { get; set; } //ubicación de la sede
    public string UbicacionActual { get; set; }
    public int TiempoTotal { get; set; }
    public Entrega[] Entregas { get; set; }
    public SiguienteEntrega Siguiente { get; set; } //null si no queda nada alcanzable
    public DestinoPendiente[] Pendientes { get; set; }
}