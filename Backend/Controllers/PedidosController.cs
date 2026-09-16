using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyHelp;
using SkyHelp.Authorization;
using SkyHelp.DTOs.Pedidos;
using SkyHelp.Repositories.Interfaces;
using SkyHelp.Services.Interfaces;

using System.Security.Claims;

namespace SkyHelp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private readonly IPedidosRepository _PedidosRepository;
        private readonly IDomiciliariosRepository _domiciliariosRepository;
        private readonly IPedidosService _pedidosService;

        public PedidosController(IPedidosRepository pedidosRepository, IDomiciliariosRepository domiciliariosRepository, IPedidosService pedidosService)
        {
            _PedidosRepository = pedidosRepository;
            _domiciliariosRepository = domiciliariosRepository;
            _pedidosService = pedidosService;
        }

        // Regla de negocio: el domiciliario confirma la entrega -> se registra la fecha/hora de
        // entrega, el pedido pasa a "Entregado" y el ticket asociado se cierra automáticamente.
        [Authorize(Roles = $"{RoleNames.Administrador},{RoleNames.Domiciliario}")]
        [HttpPost("ConfirmarEntrega")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmarEntrega([FromBody] ConfirmarEntregaRequest request)
        {
            try
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idUsuario))
                    return Unauthorized();

                var esAdmin = User.IsInRole(RoleNames.Administrador);
                var resultado = await _pedidosService.ConfirmarEntregaAsync(request.IdTicket, idUsuario, esAdmin, HttpContext.Connection.RemoteIpAddress?.ToString());

                if (resultado.NoEncontrado) return NotFound(resultado.Mensaje);
                if (resultado.NoAutorizado) return Forbid();
                if (!resultado.Exitoso) return BadRequest(resultado.Mensaje);

                return Ok("Entrega confirmada exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al confirmar la entrega.");
            }
        }

        // Regla de negocio: el Administrador solo puede asignar un domiciliario a un ticket una vez el
        // técnico registró el diagnóstico. Al asignar, nace (o se actualiza) el Pedido correspondiente.
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPost("AsignarDomiciliario")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AsignarDomiciliario([FromBody] AsignarDomiciliarioRequest request)
        {
            try
            {
                if (request == null || request.IdTicket == Guid.Empty || request.IdDomiciliario == Guid.Empty)
                    return BadRequest("El ticket y el domiciliario son obligatorios.");

                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idUsuario))
                    return Unauthorized();

                var resultado = await _pedidosService.AsignarDomiciliarioAsync(
                    request.IdTicket, request.IdDomiciliario, idUsuario, HttpContext.Connection.RemoteIpAddress?.ToString());

                if (resultado.NoEncontrado) return NotFound(resultado.Mensaje);
                if (resultado.DiagnosticoPendiente) return BadRequest(resultado.Mensaje);
                if (resultado.DomiciliarioInvalido) return BadRequest(resultado.Mensaje);
                if (!resultado.Exitoso) return BadRequest(resultado.Mensaje);

                return Ok("Domiciliario asignado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al asignar el domiciliario.");
            }
        }

        // Entregas (activas + historial) del domiciliario autenticado, con número de pedido y datos de
        // ticket/cliente ya resueltos — el domiciliario nunca ve el Guid interno ni la lista de Usuarios.
        [Authorize(Roles = RoleNames.Domiciliario)]
        [HttpGet("MisEntregas")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MisEntregas()
        {
            try
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idUsuario))
                    return Unauthorized();

                var domi = await _domiciliariosRepository.ObtenerDomiciliarioPorIdUsuario(idUsuario);
                if (domi == null)
                    return NotFound("No hay registro de domiciliario vinculado a este usuario.");

                var entregas = await _pedidosService.ObtenerEntregasDomiciliarioAsync(domi.IdDomiciliario);
                return Ok(entregas);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener las entregas.");
            }
        }

        [Authorize(Roles = RoleNames.Administrador)]
        [HttpGet("ObtenerPedidos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerPedidos()
        {
            try
            {
                var pedidos = await _PedidosRepository.ObtenerPedidos();
                if (pedidos == null || !pedidos.Any())
                {
                    return NotFound("No se encontraron pedidos.");
                }
                return Ok(pedidos);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los pedidos.");
            }
        }

        [Authorize(Roles = RoleNames.Domiciliario)]
        [HttpGet("ObtenerPedidosAsignadosDomi")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerPedidosAsignadosDomi()
        {
            try
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idUsuario))
                    return Unauthorized();

                var domi = await _domiciliariosRepository.ObtenerDomiciliarioPorIdUsuario(idUsuario);
                if (domi == null)
                    return NotFound("No hay registro de domiciliario vinculado a este usuario.");

                var pedidos = await _PedidosRepository.ObtenerPedidosPorDomiciliario(domi.IdDomiciliario);
                return Ok(pedidos);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los pedidos asignados.");
            }
        }

        [Authorize(Roles = $"{RoleNames.Administrador},{RoleNames.Domiciliario}")]
        [HttpGet("ObtenerPorId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerPedidosPorId(Guid Id)
        {
            try
            {
                var Pedido = await _PedidosRepository.ObtenerPedidoPorId(Id);
                if (Pedido == null)
                {
                    return NotFound("Pedido no encontrado.");
                }

                if (User.IsInRole(RoleNames.Domiciliario) && !User.IsInRole(RoleNames.Administrador))
                {
                    var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idUsuario))
                        return Unauthorized();
                    var domi = await _domiciliariosRepository.ObtenerDomiciliarioPorIdUsuario(idUsuario);
                    if (domi == null || Pedido.IdDomiciliario != domi.IdDomiciliario)
                        return Forbid();
                }

                return Ok(Pedido);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error al obtener el Pedido.");
            }
        }

        [Authorize(Roles = $"{RoleNames.Administrador},{RoleNames.Usuario}")]
        [HttpPost("CrearPedido")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CrearPedido([FromBody] Pedidos pedido)
        {
            try
            {
                if (User.IsInRole(RoleNames.Usuario) && !User.IsInRole(RoleNames.Administrador))
                {
                    var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idUsuario))
                        return Unauthorized();
                    pedido.IdUsuario = idUsuario;
                }

                var resultado = await _PedidosRepository.CrearPedido(pedido);
                if (!resultado)
                {
                    return BadRequest("No se puede crear el pedido.");
                }
                return Ok("Pedido Creado");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear el pedido.");
            }
        }

        [Authorize(Roles = $"{RoleNames.Administrador},{RoleNames.Domiciliario}")]
        [HttpPut("ActualizarPedido")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActualizarPedido([FromBody] Pedidos pedido)
        {
            try
            {
                if (User.IsInRole(RoleNames.Domiciliario) && !User.IsInRole(RoleNames.Administrador))
                {
                    var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idUsuario))
                        return Unauthorized();
                    var domi = await _domiciliariosRepository.ObtenerDomiciliarioPorIdUsuario(idUsuario);
                    var existente = await _PedidosRepository.ObtenerPedidoPorId(pedido.IdPedido);
                    if (domi == null || existente == null || existente.IdDomiciliario != domi.IdDomiciliario)
                        return Forbid();
                }

                var resultado = await _PedidosRepository.ActualizarPedido(pedido);
                if (!resultado)
                {
                    return BadRequest("No se puede actualizar el pedido.");
                }
                return Ok("Pedido Actualizado");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el pedido.");
            }
        }

        [Authorize(Roles = RoleNames.Administrador)]
        [HttpDelete("EliminarPedido")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EliminarPedido(Guid Id)
        {
            try
            {
                var resultado = await _PedidosRepository.EliminarPedido(Id);
                if (!resultado)
                {
                    return BadRequest("No se puede eliminar el pedido.");
                }
                return Ok("Pedido Eliminado");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar el pedido.");
            }
        }
    }
}
