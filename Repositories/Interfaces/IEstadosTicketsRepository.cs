using SkyHelp.Models;

namespace SkyHelp.Repositories.Interfaces
{
    public interface IEstadosTicketsRepository
    {
        Task<List<EstadosTicket>> ObtenerEstadosTickets();
        Task<EstadosTicket> ObtenerEstadoTicketPorID(Guid id);
        Task<bool> CrearEstadoTicket(EstadosTicket estadoTicket);
        Task<bool> ActualizarEstadoTicket(EstadosTicket estadoTicket);
        Task<bool> EliminarEstadoTicket(Guid id);
    }
}
