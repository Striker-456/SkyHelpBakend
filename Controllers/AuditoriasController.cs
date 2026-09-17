using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyHelp.Authorization;
using SkyHelp.DTOs.Auditoria;
using SkyHelp.Services.Interfaces;

namespace SkyHelp.Controllers
{
    // La auditoría es un registro de trazabilidad: solo se lee por API.
    // La escritura ocurre exclusivamente a través de IAuditoriaService, invocado internamente
    // por las acciones que se quieren auditar (login, CRUD de usuarios/tickets/técnicos, etc.).
    [Authorize(Roles = RoleNames.Administrador)]
    [Route("api/[controller]")]
    [ApiController]
    public class AuditoriasController : ControllerBase
    {
        private readonly IAuditoriaService _auditoriaService;
        public AuditoriasController(IAuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        [HttpGet("ObtenerAuditorias")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerAuditorias([FromQuery] AuditoriaFiltroDto filtro)
        {
            try
            {
                var auditorias = await _auditoriaService.ObtenerAsync(filtro);
                return Ok(auditorias);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener auditorías.");
            }
        }

        [HttpGet("ObtenerAuditoriaPorID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerAuditoriaPorId(Guid id)
        {
            try
            {
                var auditoria = await _auditoriaService.ObtenerPorIdAsync(id);
                if (auditoria == null)
                    return NotFound("Auditoría no encontrada.");
                return Ok(auditoria);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener la auditoría.");
            }
        }
    }
}
