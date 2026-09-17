using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;

namespace SkyHelp.Repositories
{
    public class ProgresoTicketsRepository : IProgresoTicketsRepository
    {
        private readonly SkyHelpContext _context;
        private readonly ILogger<ProgresoTicketsRepository> _logger;

        public ProgresoTicketsRepository(SkyHelpContext context, ILogger<ProgresoTicketsRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ProgresoTickets>> ObtenerHistorialPorTicket(Guid idTicket)
        {
            return await _context.ProgresoTickets
                .Where(p => p.IdTicket == idTicket)
                .OrderByDescending(p => p.FechaRegistro)
                .ToListAsync();
        }

        public async Task<ProgresoTickets?> ObtenerUltimoPorTicket(Guid idTicket)
        {
            return await _context.ProgresoTickets
                .Where(p => p.IdTicket == idTicket)
                .OrderByDescending(p => p.FechaRegistro)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> RegistrarProgreso(ProgresoTickets progreso)
        {
            try
            {
                await _context.ProgresoTickets.AddAsync(progreso);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar el progreso del ticket {IdTicket}", progreso.IdTicket);
                return false;
            }
        }
    }
}
