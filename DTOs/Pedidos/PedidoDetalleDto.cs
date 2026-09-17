namespace SkyHelp.DTOs.Pedidos
{
    // Vista enriquecida de un Pedido para el domiciliario: nunca expone el Guid interno como
    // identificador visible, y trae ya resuelto el nombre del cliente (el domiciliario no tiene
    // acceso al listado completo de Usuarios).
    public class PedidoDetalleDto
    {
        public Guid IdPedido { get; set; }
        public int NumeroPedido { get; set; }
        public Guid IdTicket { get; set; }
        public int NumeroTicket { get; set; }
        public Guid IdEstadoTicket { get; set; }
        public string DescripcionTicket { get; set; } = "";
        public string CategoriaTicket { get; set; } = "";
        public string ClienteNombre { get; set; } = "";
        public string EstadoPedido { get; set; } = "";
        public DateTime? FechaPedido { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string DireccionEntrega { get; set; } = "";
    }
}
