using SkyHelp.Models;

namespace SkyHelp.Repositories.Interfaces
{
    public interface ITicketsRepository
    {
        Task<List<Tickets>> ObtenerTickets();
        Task<List<Tickets>> ObtenerTicketsPorUsuario(Guid idUsuario);
        Task<List<Tickets>> ObtenerTicketsPorTecnico(Guid idTecnico);
        Task<Tickets> ObtenerTicketPorId(Guid id);
        Task<bool> CrearTicket(Tickets ticket);
        Task<bool> ActualizarTicket(Tickets ticket);
        Task<bool> ActualizarDomiciliarioTicket(Guid idTicket, Guid? idDomiciliario);
        Task<bool> ActualizarEstadoTicket(Guid idTicket, Guid idEstado);
        Task<bool> AsignarTecnico(Guid idTicket, Guid idTecnico);
        Task<bool> IniciarDiagnostico(Guid idTicket);
        Task<bool> RegistrarDiagnostico(Guid idTicket, string diagnostico, string? fallaEncontrada = null, string? pruebasRealizadas = null, string? observaciones = null, string? recomendaciones = null);
        Task<bool> ActualizarDetallesTicket(Guid idTicket, string categoria, string prioridad, string descripcion);
        Task<bool> EliminarTicket(Guid id);
    }
}
