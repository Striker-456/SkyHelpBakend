using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;

namespace SkyHelp.Repositories
{
    public class DomiciliariosRepository : IDomiciliariosRepository
    {
        private readonly SkyHelpContext _context; // Inyección de dependencia del contexto de la base de datos
        private readonly ILogger<DomiciliariosRepository> _logger;

        public DomiciliariosRepository(SkyHelpContext context, ILogger<DomiciliariosRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Domiciliarios>> ObtenerDomiciliarios()
        {
            return await _context.Domiciliarios.ToListAsync();
        }

        public async Task<Domiciliarios?> ObtenerDomiciliarioPorIdUsuario(Guid idUsuario)
        {
            return await _context.Domiciliarios.FirstOrDefaultAsync(x => x.IDUsuario == idUsuario);
        }

        public async Task<Domiciliarios> ObtenerDomiciliarioPorID(Guid id)
        {
            return await _context.Domiciliarios.FirstOrDefaultAsync(x => x.IdDomiciliario == id);
        }

        public async Task<bool> CrearDomiciliario(Domiciliarios domiciliario)
        {
            try
            {
                await _context.Domiciliarios.AddAsync(domiciliario);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el domiciliario para el usuario {IDUsuario}", domiciliario.IDUsuario);
                return false;
            }
        }

        public async Task<bool> ActualizarDomiciliario(Domiciliarios domiciliario)
        {
            try
            {
                var domiciliarioExistente = await _context.Domiciliarios
                    .FirstOrDefaultAsync(x => x.IdDomiciliario == domiciliario.IdDomiciliario);

                if (domiciliarioExistente == null)
                {
                    return false;
                }

                domiciliarioExistente.NombreCompleto = domiciliario.NombreCompleto;
                domiciliarioExistente.Telefono = domiciliario.Telefono;
                domiciliarioExistente.Email = domiciliario.Email;
                domiciliarioExistente.EstadoActividad = domiciliario.EstadoActividad;
                domiciliarioExistente.IDUsuario = domiciliario.IDUsuario;

                _context.Domiciliarios.Update(domiciliarioExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el domiciliario {IdDomiciliario}", domiciliario.IdDomiciliario);
                return false;
            }
        }

        public async Task<bool> EliminarDomiciliario(Guid id)
        {
            try
            {
                var domiciliarioExistente = await _context.Domiciliarios.FirstOrDefaultAsync(x => x.IdDomiciliario == id);

                if (domiciliarioExistente == null)
                {
                    return false;
                }

                // Desasignar tickets asociados al domiciliario (no eliminarlos)
                var ticketsAsociados = await _context.Tickets.Where(t => t.IdDomiciliario == id).ToListAsync();
                foreach (var ticket in ticketsAsociados)
                {
                    ticket.IdDomiciliario = null;
                }
                if (ticketsAsociados.Any())
                {
                    _context.Tickets.UpdateRange(ticketsAsociados);
                    await _context.SaveChangesAsync(); // Guardar primero los cambios en tickets
                }

                // Eliminar pedidos asociados al domiciliario
                var pedidosAsociados = await _context.Pedidos.Where(p => p.IdDomiciliario == id).ToListAsync();
                if (pedidosAsociados.Any())
                {
                    _context.Pedidos.RemoveRange(pedidosAsociados);
                    await _context.SaveChangesAsync(); // Guardar eliminación de pedidos
                }

                // Finalmente, eliminar el domiciliario
                _context.Domiciliarios.Remove(domiciliarioExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el domiciliario {IdDomiciliario}", id);
                throw;
            }
        }
    }
}
