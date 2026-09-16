using SkyHelp.DTOs.Pedidos;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;
using SkyHelp.Services.Interfaces;

namespace SkyHelp.Services
{
    public class PedidosService : IPedidosService
    {
        private readonly ITicketsRepository _ticketsRepository;
        private readonly IPedidosRepository _pedidosRepository;
        private readonly IDomiciliariosRepository _domiciliariosRepository;
        private readonly IEstadosTicketsRepository _estadosTicketsRepository;
        private readonly IUsuariosRepository _usuariosRepository;
        private readonly IAuditoriaService _auditoriaService;

        public PedidosService(
            ITicketsRepository ticketsRepository,
            IPedidosRepository pedidosRepository,
            IDomiciliariosRepository domiciliariosRepository,
            IEstadosTicketsRepository estadosTicketsRepository,
            IUsuariosRepository usuariosRepository,
            IAuditoriaService auditoriaService)
        {
            _ticketsRepository = ticketsRepository;
            _pedidosRepository = pedidosRepository;
            _domiciliariosRepository = domiciliariosRepository;
            _estadosTicketsRepository = estadosTicketsRepository;
            _usuariosRepository = usuariosRepository;
            _auditoriaService = auditoriaService;
        }

        // Usa la dirección que el cliente indicó al crear el ticket; si el ticket no tiene una
        // (tickets creados antes de que existiera el campo, o que la dejaron vacía por ser
        // opcional), conserva el placeholder anterior en vez de dejarlo en blanco.
        private static string ObtenerDireccionEntrega(Tickets ticket) =>
            string.IsNullOrWhiteSpace(ticket.DireccionEntrega) ? "Sin dirección registrada en el ticket" : ticket.DireccionEntrega;

        public async Task<ConfirmarEntregaResultado> ConfirmarEntregaAsync(Guid idTicket, Guid idUsuarioActor, bool actorEsAdmin, string? ip)
        {
            var ticket = await _ticketsRepository.ObtenerTicketPorId(idTicket);
            if (ticket == null)
                return new ConfirmarEntregaResultado { NoEncontrado = true, Mensaje = "Ticket no encontrado." };

            if (!actorEsAdmin)
            {
                var domiciliario = await _domiciliariosRepository.ObtenerDomiciliarioPorIdUsuario(idUsuarioActor);
                if (domiciliario == null || ticket.IdDomiciliario != domiciliario.IdDomiciliario)
                    return new ConfirmarEntregaResultado { NoAutorizado = true, Mensaje = "No tiene permisos sobre este ticket." };
            }

            if (ticket.IdDomiciliario == null)
                return new ConfirmarEntregaResultado { Mensaje = "El ticket no tiene un domiciliario asignado." };

            var ahora = DateTime.UtcNow;
            var pedido = await _pedidosRepository.ObtenerPedidoPorIdTicket(idTicket);

            if (pedido != null)
            {
                pedido.EstadoPedido = "Entregado";
                pedido.FechaEntrega = ahora;
                await _pedidosRepository.ActualizarPedido(pedido);
            }
            else
            {
                pedido = new Pedidos
                {
                    IdUsuario = ticket.IdUsuario,
                    IdDomiciliario = ticket.IdDomiciliario.Value,
                    IdTicket = idTicket,
                    FechaPedido = ticket.FechaCreacion ?? ahora,
                    FechaEntrega = ahora,
                    DireccionEntrega = ObtenerDireccionEntrega(ticket),
                    EstadoPedido = "Entregado",
                    Observaciones = string.Empty
                };
                await _pedidosRepository.CrearPedido(pedido);
            }

            // Cerrar automáticamente el ticket asociado (esto también marca Tickets.FechaCierre,
            // ver TicketsRepository.ActualizarEstadoTicket).
            var estados = await _estadosTicketsRepository.ObtenerEstadosTickets();
            var estadoResuelto = estados.FirstOrDefault(e => e.NombreEstado.Contains("resuelto", StringComparison.OrdinalIgnoreCase));
            if (estadoResuelto != null)
                await _ticketsRepository.ActualizarEstadoTicket(idTicket, estadoResuelto.IdEstado);

            await _auditoriaService.RegistrarAsync(idUsuarioActor, "Actualizar", "Pedidos", pedido.IdPedido,
                $"Entrega confirmada para el ticket #{ticket.NumeroTicket}: pedido marcado como Entregado y ticket cerrado.", ip);

            return new ConfirmarEntregaResultado { Exitoso = true };
        }

        public async Task<AsignarDomiciliarioResultado> AsignarDomiciliarioAsync(Guid idTicket, Guid idDomiciliario, Guid idUsuarioActor, string? ip)
        {
            var ticket = await _ticketsRepository.ObtenerTicketPorId(idTicket);
            if (ticket == null)
                return new AsignarDomiciliarioResultado { NoEncontrado = true, Mensaje = "Ticket no encontrado." };

            if (string.IsNullOrWhiteSpace(ticket.Diagnostico))
                return new AsignarDomiciliarioResultado
                {
                    DiagnosticoPendiente = true,
                    Mensaje = "El técnico debe registrar el diagnóstico antes de asignar un domiciliario."
                };

            var domiciliario = await _domiciliariosRepository.ObtenerDomiciliarioPorID(idDomiciliario);
            if (domiciliario == null)
                return new AsignarDomiciliarioResultado { DomiciliarioInvalido = true, Mensaje = "El domiciliario indicado no existe." };

            await _ticketsRepository.ActualizarDomiciliarioTicket(idTicket, idDomiciliario);

            var pedido = await _pedidosRepository.ObtenerPedidoPorIdTicket(idTicket);
            if (pedido != null)
            {
                pedido.IdDomiciliario = idDomiciliario;
                await _pedidosRepository.ActualizarPedido(pedido);
            }
            else
            {
                pedido = new Pedidos
                {
                    IdUsuario = ticket.IdUsuario,
                    IdDomiciliario = idDomiciliario,
                    IdTicket = idTicket,
                    FechaPedido = DateTime.UtcNow,
                    DireccionEntrega = ObtenerDireccionEntrega(ticket),
                    EstadoPedido = "Asignado",
                    Observaciones = string.Empty
                };
                await _pedidosRepository.CrearPedido(pedido);
            }

            await _auditoriaService.RegistrarAsync(idUsuarioActor, "Actualizar", "Pedidos", pedido.IdPedido,
                $"Domiciliario asignado al pedido del ticket #{ticket.NumeroTicket}.", ip);

            return new AsignarDomiciliarioResultado { Exitoso = true };
        }

        public async Task<List<PedidoDetalleDto>> ObtenerEntregasDomiciliarioAsync(Guid idDomiciliario)
        {
            var pedidos = await _pedidosRepository.ObtenerPedidosPorDomiciliario(idDomiciliario);
            var resultado = new List<PedidoDetalleDto>();

            foreach (var pedido in pedidos)
            {
                var dto = new PedidoDetalleDto
                {
                    IdPedido = pedido.IdPedido,
                    NumeroPedido = pedido.NumeroPedido,
                    EstadoPedido = pedido.EstadoPedido,
                    FechaPedido = pedido.FechaPedido,
                    FechaEntrega = pedido.FechaEntrega,
                    DireccionEntrega = pedido.DireccionEntrega
                };

                if (pedido.IdTicket.HasValue)
                {
                    var ticket = await _ticketsRepository.ObtenerTicketPorId(pedido.IdTicket.Value);
                    if (ticket != null)
                    {
                        dto.IdTicket = ticket.IdTicket;
                        dto.NumeroTicket = ticket.NumeroTicket;
                        dto.IdEstadoTicket = ticket.IdEstado;
                        dto.DescripcionTicket = ticket.Descripcion;
                        dto.CategoriaTicket = ticket.Categoria;
                    }
                }

                var cliente = await _usuariosRepository.ObtenerUsuario(pedido.IdUsuario);
                if (cliente != null)
                    dto.ClienteNombre = cliente.NombreCompleto;

                resultado.Add(dto);
            }

            return resultado;
        }
    }
}
