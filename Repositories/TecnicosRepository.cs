using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;


namespace SkyHelp.Repositories
{
    public class TecnicosRepository : ITecnicosRepository
    {
        private readonly SkyHelpContext _context;
        private readonly ILogger<TecnicosRepository> _logger;
        public TecnicosRepository(SkyHelpContext context, ILogger<TecnicosRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<List<Tecnicos>> ObtenerTecnicos()
        {
            return await _context.Tecnicos
                .Include(t => t.Usuario)
                .ToListAsync();
        }
        public async Task<Tecnicos> ObtenerTecnicoPorId(Guid id)
        {
            return await _context.Tecnicos.FirstOrDefaultAsync(x => x.IdTecnico == id);
        }

        public async Task<Tecnicos?> ObtenerTecnicoPorIdUsuario(Guid idUsuario)
        {
            return await _context.Tecnicos.FirstOrDefaultAsync(x => x.IdUsuario == idUsuario);
        }

        public async Task<bool> CrearTecnico(Tecnicos tecnico)
        {
            try
            {
                await _context.Tecnicos.AddAsync(tecnico);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el técnico para el usuario {IdUsuario}", tecnico.IdUsuario);
                return false;
            }
        }
        public async Task<bool> ActualizarTecnico(Tecnicos tecnico)
        {
            try
            {
                var tecnicoExistente = await _context.Tecnicos.FirstOrDefaultAsync(x => x.IdTecnico == tecnico.IdTecnico);
                if (tecnicoExistente == null)
                {
                    return false;
                }
                tecnicoExistente.IdUsuario = tecnico.IdUsuario;
                tecnicoExistente.FechaRegistro = tecnico.FechaRegistro;
                _context.Tecnicos.Update(tecnicoExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el técnico {IdTecnico}", tecnico.IdTecnico);
                return false;
            }
        }
        public async Task<bool> EliminarTecnico(Guid id)
        {
            try
            {
                var tecnicoExistente = await _context.Tecnicos.FirstOrDefaultAsync(x => x.IdTecnico == id);
                if (tecnicoExistente == null)
                {
                    return false;
                }

                // Desasignar (no borrar) los tickets asociados al técnico: la FK Tickets.IdTecnico
                // es ClientSetNull, así que EF sólo la pone en null si el ticket está cargado en el
                // contexto (mismo patrón que DomiciliariosRepository.EliminarDomiciliario).
                var ticketsAsociados = await _context.Tickets.Where(t => t.IdTecnico == id).ToListAsync();
                foreach (var ticket in ticketsAsociados)
                {
                    ticket.IdTecnico = null;
                }
                if (ticketsAsociados.Count > 0)
                {
                    _context.Tickets.UpdateRange(ticketsAsociados);
                    await _context.SaveChangesAsync();
                }

                // Mismo caso para ProgresoTickets.IdTecnico (también ClientSetNull): conserva el
                // historial de progreso, solo desvincula quién lo registró.
                var progresosAsociados = await _context.ProgresoTickets.Where(p => p.IdTecnico == id).ToListAsync();
                foreach (var progreso in progresosAsociados)
                {
                    progreso.IdTecnico = null;
                }
                if (progresosAsociados.Count > 0)
                {
                    _context.ProgresoTickets.UpdateRange(progresosAsociados);
                    await _context.SaveChangesAsync();
                }

                _context.Tecnicos.Remove(tecnicoExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el técnico {IdTecnico}", id);
                return false;
            }
        }
    }
}
