using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyHelp.Authorization;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;

namespace SkyHelp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EstadosTicketsController : ControllerBase
    {
        private readonly IEstadosTicketsRepository _estadosTicketsRepository;
        public EstadosTicketsController(IEstadosTicketsRepository estadosTicketsRepository)
        {
            _estadosTicketsRepository = estadosTicketsRepository;
        }


        // OBTENER TODOS
        [HttpGet("ObtenerEstadosTickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> ObtenerEstadosTickets()
        {
            try
            {
                var estadosTickets = await _estadosTicketsRepository.ObtenerEstadosTickets();
                if (estadosTickets == null || !estadosTickets.Any())
                {
                    return NotFound("No se encontraron estados de tickets.");
                }
                return Ok(estadosTickets);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los estados de tickets.");
            }
        }

        // OBTENER POR ID
        [HttpGet("ObtenerEstadoTicketPorId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> ObtenerEstadoTicketPorId(Guid Id)
        {
            try
            {
                var estadoTicket = await _estadosTicketsRepository.ObtenerEstadoTicketPorID(Id);
                if (estadoTicket == null)
                {
                    return NotFound("Estado de ticket no encontrado.");
                }
                return Ok(estadoTicket);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el estado del ticket.");
            }
        }

        // CREAR
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPost("CrearEstadoTicket")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> CrearEstadoTicket([FromBody] EstadosTicket estadoTicket)
        {
            try
            {
                var resultado = await _estadosTicketsRepository.CrearEstadoTicket(estadoTicket);
                if (!resultado)
                {
                    return BadRequest("No se puede crear el estado del ticket.");
                }
                return Ok("Estado de ticket creado.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear el estado del ticket.");
            }
        }

        // ACTUALIZAR
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPut("ActualizarEstadoTicket")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> ActualizarEstadoTicket([FromBody] EstadosTicket estadoTicket)
        {
            try
            {
                var resultado = await _estadosTicketsRepository.ActualizarEstadoTicket(estadoTicket);
                if (!resultado)
                {
                    return BadRequest("No se puede actualizar el estado del ticket.");
                }
                return Ok("Estado de ticket actualizado.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el estado del ticket.");
            }
        }

        // ELIMINAR
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpDelete("EliminarEstadoTicket")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> EliminarEstadoTicket(Guid Id)
        {
            try
            {
                var resultado = await _estadosTicketsRepository.EliminarEstadoTicket(Id);
                if (!resultado)
                {
                    return NotFound("No se puede eliminar el estado del ticket.");
                }
                return Ok("Estado de ticket eliminado.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar el estado del ticket.");
            }
        }
    }
}

