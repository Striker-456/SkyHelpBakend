using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;

namespace SkyHelp.Repositories
{
    public class TicketsRepository : ITicketsRepository
    {
        private readonly SkyHelpContext _context;// Inyección de dependencia del contexto de la base de datos
        private readonly ILogger<TicketsRepository> _logger;
        public TicketsRepository(SkyHelpContext context, ILogger<TicketsRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<List<Tickets>> ObtenerTickets()
        {
            return await _context.Tickets.ToListAsync();
        }

        public async Task<List<Tickets>> ObtenerTicketsPorUsuario(Guid idUsuario)
        {
            return await _context.Tickets.Where(t => t.IdUsuario == idUsuario).ToListAsync();
        }

        public async Task<List<Tickets>> ObtenerTicketsPorTecnico(Guid idTecnico)
        {
            return await _context.Tickets.Where(t => t.IdTecnico == idTecnico).ToListAsync();
        }

        public async Task<Tickets> ObtenerTicketPorId(Guid id)
        {
            return await _context.Tickets.FirstOrDefaultAsync(x => x.IdTicket == id);
        }
        public async Task<bool> CrearTicket(Tickets ticket)
        {
            try
            {
                await _context.Tickets.AddAsync(ticket);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el ticket {@Ticket}", new { ticket.IdUsuario, ticket.Categoria, ticket.IdEstado });
                return false;
            }
        }
        public async Task<bool> ActualizarTicket(Tickets ticket)
        {
            try
            {
                if (ticket == null || ticket.IdTicket == Guid.Empty)
                    throw new ArgumentException("Ticket nulo o sin ID");

                // Obtener el ticket existente
                var ticketExistente = await _context.Tickets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdTicket == ticket.IdTicket);

                if (ticketExistente == null)
                    throw new KeyNotFoundException($"Ticket con ID {ticket.IdTicket} no encontrado");

                // Crear una copia con solo los campos que queremos actualizar
                var ticketActualizado = new Tickets
                {
                    IdTicket = ticketExistente.IdTicket,
                    NumeroTicket = ticketExistente.NumeroTicket,
                    FechaCreacion = ticketExistente.FechaCreacion,
                    IdUsuario = ticketExistente.IdUsuario,
                    // Actualizar solo si vienen con valores válidos
                    Descripcion = !string.IsNullOrWhiteSpace(ticket.Descripcion) ? ticket.Descripcion : ticketExistente.Descripcion,
                    Categoria = !string.IsNullOrWhiteSpace(ticket.Categoria) ? ticket.Categoria : ticketExistente.Categoria,
                    Prioridad = !string.IsNullOrWhiteSpace(ticket.Prioridad) ? ticket.Prioridad : ticketExistente.Prioridad,
                    IdEstado = ticket.IdEstado != Guid.Empty ? ticket.IdEstado : ticketExistente.IdEstado,
                    IdTecnico = ticket.IdTecnico != Guid.Empty ? ticket.IdTecnico : ticketExistente.IdTecnico,
                    IdDomiciliario = ticket.IdDomiciliario != Guid.Empty ? ticket.IdDomiciliario : ticketExistente.IdDomiciliario
                };

                _context.Tickets.Update(ticketActualizado);
                var cambios = await _context.SaveChangesAsync();
                return cambios > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el ticket {IdTicket}", ticket?.IdTicket);
                throw;
            }
        }

        public async Task<bool> ActualizarDomiciliarioTicket(Guid idTicket, Guid? idDomiciliario)
        {
            try
            {
                if (idTicket == Guid.Empty)
                    throw new ArgumentException("ID del ticket inválido");

                // Usar SQL directo para actualizar solo el domiciliario
                var resultado = await _context.Tickets
                    .Where(t => t.IdTicket == idTicket)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.IdDomiciliario, idDomiciliario));

                return resultado > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el domiciliario del ticket {IdTicket}", idTicket);
                throw;
            }
        }

        public async Task<bool> ActualizarEstadoTicket(Guid idTicket, Guid idEstado)
        {
            try
            {
                if (idTicket == Guid.Empty)
                    throw new ArgumentException("ID del ticket inválido");

                if (idEstado == Guid.Empty)
                    throw new ArgumentException("ID del estado inválido");

                var estado = await _context.EstadosTickets.AsNoTracking().FirstOrDefaultAsync(e => e.IdEstado == idEstado);
                var esEstadoTerminal = estado != null &&
                    (estado.NombreEstado.Equals("Resuelto", StringComparison.OrdinalIgnoreCase) ||
                     estado.NombreEstado.Equals("Cerrado", StringComparison.OrdinalIgnoreCase));
                DateTime? fechaCierre = esEstadoTerminal ? DateTime.UtcNow : null;

                // Usar SQL directo para actualizar solo el estado (y la fecha de cierre derivada de él)
                var resultado = await _context.Tickets
                    .Where(t => t.IdTicket == idTicket)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.IdEstado, idEstado)
                        .SetProperty(t => t.FechaCierre, fechaCierre));

                return resultado > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el estado del ticket {IdTicket}", idTicket);
                throw;
            }
        }

        public async Task<bool> AsignarTecnico(Guid idTicket, Guid idTecnico)
        {
            try
            {
                if (idTicket == Guid.Empty || idTecnico == Guid.Empty)
                    throw new ArgumentException("ID de ticket o técnico inválido");

                var resultado = await _context.Tickets
                    .Where(t => t.IdTicket == idTicket)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.IdTecnico, idTecnico));

                return resultado > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar el técnico del ticket {IdTicket}", idTicket);
                throw;
            }
        }

        public async Task<bool> IniciarDiagnostico(Guid idTicket)
        {
            try
            {
                if (idTicket == Guid.Empty)
                    throw new ArgumentException("ID del ticket inválido");

                var resultado = await _context.Tickets
                    .Where(t => t.IdTicket == idTicket)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.FechaDiagnostico, DateTime.UtcNow));

                return resultado > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar el diagnóstico del ticket {IdTicket}", idTicket);
                throw;
            }
        }

        public async Task<bool> RegistrarDiagnostico(Guid idTicket, string diagnostico, string? fallaEncontrada = null, string? pruebasRealizadas = null, string? observaciones = null, string? recomendaciones = null)
        {
            try
            {
                if (idTicket == Guid.Empty)
                    throw new ArgumentException("ID del ticket inválido");

                var resultado = await _context.Tickets
                    .Where(t => t.IdTicket == idTicket)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.Diagnostico, diagnostico)
                        .SetProperty(t => t.FechaDiagnostico, DateTime.UtcNow)
                        .SetProperty(t => t.FallaEncontrada, fallaEncontrada)
                        .SetProperty(t => t.PruebasRealizadas, pruebasRealizadas)
                        .SetProperty(t => t.Observaciones, observaciones)
                        .SetProperty(t => t.Recomendaciones, recomendaciones));

                return resultado > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar el diagnóstico del ticket {IdTicket}", idTicket);
                throw;
            }
        }

        public async Task<bool> ActualizarDetallesTicket(Guid idTicket, string categoria, string prioridad, string descripcion)
        {
            try
            {
                if (idTicket == Guid.Empty)
                    throw new ArgumentException("ID del ticket inválido");

                var resultado = await _context.Tickets
                    .Where(t => t.IdTicket == idTicket)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.Categoria, categoria)
                        .SetProperty(t => t.Prioridad, prioridad)
                        .SetProperty(t => t.Descripcion, descripcion));

                return resultado > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar los detalles del ticket {IdTicket}", idTicket);
                throw;
            }
        }

        public async Task<bool> EliminarTicket(Guid id)
        {
            try
            {
                var ticketExistente = await _context.Tickets.FirstOrDefaultAsync(x => x.IdTicket == id);
                if (ticketExistente == null)
                {
                    return false;
                }
                _context.Tickets.Remove(ticketExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el ticket {IdTicket}", id);
                return false;
            }
        }
    }
}
