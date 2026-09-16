using SkyHelp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyHelp.Authorization;
using SkyHelp.Services.Interfaces;
namespace SkyHelp.Controllers
{
    [Authorize(Roles = RoleNames.Administrador)]
    [Route("api/[controller]")]
    [ApiController]
    public class EstadisticasController : ControllerBase
    {
        private readonly IEstadisticasService _estadisticasService;
        public EstadisticasController(IEstadisticasService estadisticasService)
        {
            _estadisticasService = estadisticasService;
        }

        // Resumen agregado (KPIs, distribución, prioridad, comparativa mensual, rendimiento por técnico)
        [HttpGet("ResumenEstadisticas")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResumenEstadisticas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            try
            {
                return Ok(await _estadisticasService.ObtenerResumenAsync(desde, hasta));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al calcular el resumen de estadísticas.");
            }
        }
    }
}
