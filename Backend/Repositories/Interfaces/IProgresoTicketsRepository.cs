using SkyHelp.Models;

namespace SkyHelp.Repositories.Interfaces
{
    public interface IProgresoTicketsRepository
    {
        Task<List<ProgresoTickets>> ObtenerHistorialPorTicket(Guid idTicket);
        Task<ProgresoTickets?> ObtenerUltimoPorTicket(Guid idTicket);
        Task<bool> RegistrarProgreso(ProgresoTickets progreso);
    }
}
