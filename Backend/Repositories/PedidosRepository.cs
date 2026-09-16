using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.Repositories.Interfaces;

namespace SkyHelp.Repositories
{
    public class PedidosRepository : IPedidosRepository
    {
        private readonly SkyHelpContext _context;// Inyección de dependencia del contexto de la base de datos
        private readonly ILogger<PedidosRepository> _logger;

        public PedidosRepository(SkyHelpContext context, ILogger<PedidosRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Pedidos>> ObtenerPedidos()
        {
            return await _context.Pedidos.ToListAsync();
        }

        public async Task<List<Pedidos>> ObtenerPedidosPorDomiciliario(Guid idDomiciliario)
        {
            return await _context.Pedidos.Where(p => p.IdDomiciliario == idDomiciliario).ToListAsync();
        }

        public async Task<Pedidos?> ObtenerPedidoPorIdTicket(Guid idTicket)
        {
            return await _context.Pedidos.FirstOrDefaultAsync(x => x.IdTicket == idTicket);
        }

        public async Task<Pedidos> ObtenerPedidoPorId(Guid id)
        {
            return await _context.Pedidos.FirstOrDefaultAsync(x => x.IdPedido == id);
        }
        public async Task<bool> CrearPedido(Pedidos pedido)
        {
            try
            {
                await _context.Pedidos.AddAsync(pedido);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el pedido para el usuario {IdUsuario}", pedido.IdUsuario);
                return false;
            }
        }
        public async Task<bool> ActualizarPedido(Pedidos pedido)
        {
            try
            {
                var pedidoExistente = await _context.Pedidos.FirstOrDefaultAsync(x => x.IdPedido == pedido.IdPedido);
                if (pedidoExistente == null)
                {
                    return false;
                }
                pedidoExistente.IdUsuario = pedido.IdUsuario;
                pedidoExistente.IdDomiciliario = pedido.IdDomiciliario;
                pedidoExistente.DireccionEntrega = pedido.DireccionEntrega;
                pedidoExistente.EstadoPedido = pedido.EstadoPedido;
                pedidoExistente.Observaciones = pedido.Observaciones;
                pedidoExistente.FechaPedido = pedido.FechaPedido;
                pedidoExistente.IdTicket = pedido.IdTicket;
                pedidoExistente.FechaEntrega = pedido.FechaEntrega;
                // Sin llamar a Update(): la entidad ya está siendo rastreada por el contexto (se obtuvo
                // sin AsNoTracking), así que SaveChangesAsync ya detecta los cambios reales. Llamar a
                // Update() aquí marcaba TODAS las propiedades como modificadas, incluyendo NumeroPedido
                // (columna IDENTITY), y SQL Server rechazaba el UPDATE resultante.
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el pedido {IdPedido}", pedido.IdPedido);
                return false;
            }
        }

        public async Task<bool> EliminarPedido(Guid id)
        {
            try
            {
                var pedidoExistente = await _context.Pedidos.FirstOrDefaultAsync(x => x.IdPedido == id);
                if (pedidoExistente == null)
                {
                    return false;
                }
                _context.Pedidos.Remove(pedidoExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el pedido {IdPedido}", id);
                return false;
            }
        }
    }
}
