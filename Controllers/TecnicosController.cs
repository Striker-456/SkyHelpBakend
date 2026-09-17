using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyHelp.Authorization;
using SkyHelp.Repositories.Interfaces;
using SkyHelp.Services.Interfaces;
using System.Security.Claims;

namespace SkyHelp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TecnicosController : ControllerBase
    {
        private readonly ITecnicosRepository _tecnicosRepository;
        private readonly IAuditoriaService _auditoriaService;
        public TecnicosController(ITecnicosRepository tecnicosRepository, IAuditoriaService auditoriaService)
        {
            _tecnicosRepository = tecnicosRepository;
            _auditoriaService = auditoriaService;
        }

        private string? ObtenerIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private Guid ObtenerIdActor()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        }
        // OBTENER TODOS
        [Authorize]
        [HttpGet("ObtenerTecnicos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerTecnicos()
        {
            try
            {
                var lista = await _tecnicosRepository.ObtenerTecnicos();
                
                var resultado = (lista ?? new List<Tecnicos>()).Select(t => new {
                    t.IdTecnico,
                    t.IdUsuario,
                    t.FechaRegistro,
                    NombreCompleto = t.Usuario?.NombreCompleto ?? t.Usuario?.NombreUsuarios ?? "",
                    Correo = t.Usuario?.Correo ?? ""
                });
                return Ok(resultado);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los técnicos.");
            }
        }
        // OBTENER POR ID
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpGet("ObtenerTecnicoPorID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerTecnicoPorID(Guid id)
        {
            try
            {
                var tecnico = await _tecnicosRepository.ObtenerTecnicoPorId(id);
                if (tecnico == null)
                {
                    return NotFound("Técnico no encontrado.");
                }
                return Ok(tecnico);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el técnico.");
            }
        }
        // CREAR
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPost("CrearTecnico")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CrearTecnico([FromBody] Tecnicos tecnico)
        {
            try
            {
                var resultado = await _tecnicosRepository.CrearTecnico(tecnico);
                if (!resultado)
                {
                    return BadRequest("No se pudo crear el técnico.");
                }
                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Crear", "Técnicos", tecnico.IdTecnico,
                    "Técnico creado.", ObtenerIp());
                return Ok("Técnico creado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear el técnico.");
            }
        }
        // ACTUALIZAR
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPut("ActualizarTecnico")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActualizarTecnico([FromBody] Tecnicos tecnico)
        {
            try
            {
                var resultado = await _tecnicosRepository.ActualizarTecnico(tecnico);
                if (!resultado)
                {
                    return NotFound("No se pudo actualizar el técnico.");
                }
                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Actualizar", "Técnicos", tecnico.IdTecnico,
                    "Técnico actualizado.", ObtenerIp());
                return Ok("Técnico actualizado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el técnico.");
            }
        }
        // ELIMINAR
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpDelete("EliminarTecnico")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EliminarTecnico(Guid id)
        {
            try
            {
                var resultado = await _tecnicosRepository.EliminarTecnico(id);
                if (!resultado)
                {
                    return NotFound("No se pudo eliminar el técnico.");
                }
                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Eliminar", "Técnicos", id,
                    "Técnico eliminado.", ObtenerIp());
                return Ok("Técnico eliminado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar el técnico.");
            }
        }
    }
}
