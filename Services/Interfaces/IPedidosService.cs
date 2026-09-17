using SkyHelp.DTOs.Pedidos;

namespace SkyHelp.Services.Interfaces
{
    public interface IPedidosService
    {
        // Regla de negocio: el domiciliario confirma que el equipo fue entregado -> se registra la
        // fecha/hora de entrega, el pedido pasa a "Entregado" y el ticket asociado se cierra solo.
        Task<ConfirmarEntregaResultado> ConfirmarEntregaAsync(Guid idTicket, Guid idUsuarioActor, bool actorEsAdmin, string? ip);

        // Regla de negocio: el Administrador solo puede asignar un domiciliario a un ticket una vez
        // el técnico ya registró el diagnóstico. Al asignar nace (o se actualiza) el Pedido asociado.
        Task<AsignarDomiciliarioResultado> AsignarDomiciliarioAsync(Guid idTicket, Guid idDomiciliario, Guid idUsuarioActor, string? ip);

        // Entregas (activas + historial) del domiciliario autenticado, con el número de pedido y los
        // datos del ticket/cliente ya resueltos server-side.
        Task<List<PedidoDetalleDto>> ObtenerEntregasDomiciliarioAsync(Guid idDomiciliario);
    }
}
