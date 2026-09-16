using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyHelp.Authorization;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;
using System.Security.Claims;

namespace SkyHelp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DomiciliariosController : ControllerBase
    {
        private readonly IDomiciliariosRepository _domiciliariosRepository;

        public DomiciliariosController(IDomiciliariosRepository domiciliariosRepository)
        {
            _domiciliariosRepository = domiciliariosRepository;
        }

        private Guid ObtenerIdActor()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        }

        // OBTENER TODOS
        [Authorize]
        [HttpGet("ObtenerDomiciliarios")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerDomiciliarios()
        {
            try
            {
                var lista = await _domiciliariosRepository.ObtenerDomiciliarios();
                return Ok(lista ?? new List<Domiciliarios>());
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los domiciliarios.");
            }
        }

        // OBTENER POR ID
        [HttpGet("ObtenerDomiciliarioPorID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerDomiciliarioPorID(Guid id)
        {
            try
            {
                var domiciliario = await _domiciliariosRepository.ObtenerDomiciliarioPorID(id);

                if (domiciliario == null)
                {
                    return NotFound("Domiciliario no encontrado.");
                }

                return Ok(domiciliario);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el domiciliario.");
            }
        }

        // OBTENER DOMICILIARIO ACTUAL (del usuario autenticado)
        [Authorize]
        [HttpGet("ObtenerDomiciliarioActual")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerDomiciliarioActual()
        {
            try
            {
                // Obtener el ID del usuario autenticado
                var idUsuarioStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idUsuarioStr) || !Guid.TryParse(idUsuarioStr, out var idUsuario))
                {
                    return Unauthorized("No se pudo obtener el ID del usuario.");
                }

                // Obtener todos los domiciliarios y buscar el que coincida con el usuario
                var domiciliarios = await _domiciliariosRepository.ObtenerDomiciliarios();
                var domiciliarioActual = domiciliarios?.FirstOrDefault(d => d.IDUsuario == idUsuario);

                if (domiciliarioActual == null)
                {
                    return NotFound("No se encontró un domiciliario asociado a este usuario.");
                }

                return Ok(domiciliarioActual);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el domiciliario actual.");
            }
        }

       
        // CREAR
        [HttpPost("CrearDomiciliario")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CrearDomiciliario([FromBody] Domiciliarios domiciliario)
        {
            try
            {
                // El auto-registro como domiciliario solo puede crear SU PROPIO registro; un admin
                // puede crear el registro de cualquier usuario.
                if (!User.IsInRole(RoleNames.Administrador))
                    domiciliario.IDUsuario = ObtenerIdActor();

                var resultado = await _domiciliariosRepository.CrearDomiciliario(domiciliario);

                if (!resultado)
                {
                    return BadRequest("No se pudo crear el domiciliario.");
                }

                return Ok("Domiciliario creado exitosamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear el domiciliario.");
            }
        }

        // ACTUALIZAR
        [HttpPut("ActualizarDomiciliario")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActualizarDomiciliario([FromBody] Domiciliarios domiciliario)
        {
            try
            {
                var existente = await _domiciliariosRepository.ObtenerDomiciliarioPorID(domiciliario.IdDomiciliario);
                if (existente == null)
                    return NotFound("No se pudo actualizar el domiciliario.");
                var esAdmin = User.IsInRole(RoleNames.Administrador);
                if (!esAdmin && existente.IDUsuario != ObtenerIdActor())
                    return Forbid();
                // Un domiciliario no puede reasignar su registro a otro usuario.
                if (!esAdmin)
                    domiciliario.IDUsuario = existente.IDUsuario;

                var resultado = await _domiciliariosRepository.ActualizarDomiciliario(domiciliario);

                if (!resultado)
                {
                    return NotFound("No se pudo actualizar el domiciliario.");
                }

                return Ok("Domiciliario actualizado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el domiciliario.");
            }
        }

        // ELIMINAR

        [Authorize(Roles = RoleNames.Administrador)]
        [HttpDelete("EliminarDomiciliario")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EliminarDomiciliario(Guid id)
        {
            try
            {
                var resultado = await _domiciliariosRepository.EliminarDomiciliario(id);

                if (!resultado)
                {
                    return NotFound("No se pudo eliminar el domiciliario.");
                }

                return Ok("Domiciliario eliminado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar el domiciliario.");
            }
        }
    }
}
    
