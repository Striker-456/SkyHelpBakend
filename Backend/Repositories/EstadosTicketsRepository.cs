using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;

namespace SkyHelp.Repositories
{
    public class EstadosTicketsRepository : IEstadosTicketsRepository
    {
        private readonly SkyHelpContext _context;// Inyección de dependencia del contexto de la base de datos
        private readonly ILogger<EstadosTicketsRepository> _logger;
        public EstadosTicketsRepository(SkyHelpContext context, ILogger<EstadosTicketsRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<EstadosTicket>> ObtenerEstadosTickets()
        {
            return await _context.EstadosTickets.ToListAsync();
        }
        public async Task<EstadosTicket> ObtenerEstadoTicketPorID(Guid id)
        {
            return await _context.EstadosTickets.FirstOrDefaultAsync(x => x.IdEstado == id);
        }
        public async Task<bool> CrearEstadoTicket(EstadosTicket estadoTicket)
        {
            try
            {
                await _context.EstadosTickets.AddAsync(estadoTicket);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el estado de ticket {NombreEstado}", estadoTicket.NombreEstado);
                return false;
            }
        }

        public async Task<bool> ActualizarEstadoTicket(EstadosTicket estadoTicket)
        {
            try
            {
                var estadoTicketExistente = await _context.EstadosTickets.FirstOrDefaultAsync(x => x.IdEstado == estadoTicket.IdEstado);
                if (estadoTicketExistente == null)
                {
                    return false;
                }
                estadoTicketExistente.NombreEstado = estadoTicket.NombreEstado;
                estadoTicketExistente.Descripcion = estadoTicket.Descripcion;
                _context.EstadosTickets.Update(estadoTicketExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el estado de ticket {IdEstado}", estadoTicket.IdEstado);
                return false;
            }
        }

        public async Task<bool> EliminarEstadoTicket(Guid id)
        {
            try
            {
                var estadoTicketExistente = await _context.EstadosTickets.FirstOrDefaultAsync(x => x.IdEstado == id);
                if (estadoTicketExistente == null)
                {
                    return false;
                }
                _context.EstadosTickets.Remove(estadoTicketExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el estado de ticket {IdEstado}", id);
                return false;
            }
        }
    }
}
