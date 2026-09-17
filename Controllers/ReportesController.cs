using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyHelp.Authorization;
using SkyHelp.DTOs.Reportes;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;
using SkyHelp.Services.Interfaces;
using System.Security.Claims;

namespace SkyHelp.Controllers
{
    [Authorize(Roles = RoleNames.Administrador)]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportesController : ControllerBase
    {
        private readonly IReportesRepository _ReportesRepository;
        private readonly IReportesService _reportesService;
        private readonly IReporteExportService _exportService;
        private readonly IAuditoriaService _auditoriaService;

        public ReportesController(
            IReportesRepository ReportesRepository,
            IReportesService reportesService,
            IReporteExportService exportService,
            IAuditoriaService auditoriaService)
        {
            _ReportesRepository = ReportesRepository;
            _reportesService = reportesService;
            _exportService = exportService;
            _auditoriaService = auditoriaService;
        }

        private Guid ObtenerIdActor()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        }

        private string? ObtenerIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        // Genera un reporte real (agregación de datos) a partir de un tipo + filtros.
        [HttpPost("GenerarReporte")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerarReporte([FromBody] GenerarReporteRequest request)
        {
            try
            {
                var idActor = ObtenerIdActor();
                var reporte = await _reportesService.GenerarAsync(request, idActor);
                await _auditoriaService.RegistrarAsync(idActor, "Crear", "Reportes", reporte.IdReporte,
                    $"{reporte.Titulo} generado ({reporte.Filas.Count} registros).", ObtenerIp());
                return Ok(reporte);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al generar el reporte.");
            }
        }

        // Últimos reportes generados, para la tabla "Reportes recientes".
        [HttpGet("ObtenerReportesRecientes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerReportesRecientes()
        {
            try
            {
                return Ok(await _reportesService.ObtenerRecientesAsync());
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los reportes recientes.");
            }
        }

        [HttpGet("ExportarCsv/{idReporte:guid}")]
        public Task<IActionResult> ExportarCsv(Guid idReporte) => ExportarComo(idReporte, "csv");

        [HttpGet("ExportarExcel/{idReporte:guid}")]
        public Task<IActionResult> ExportarExcel(Guid idReporte) => ExportarComo(idReporte, "excel");

        [HttpGet("ExportarPdf/{idReporte:guid}")]
        public Task<IActionResult> ExportarPdf(Guid idReporte) => ExportarComo(idReporte, "pdf");

        private async Task<IActionResult> ExportarComo(Guid idReporte, string formato)
        {
            try
            {
                var reporte = await _reportesService.ObtenerGeneradoPorIdAsync(idReporte);
                if (reporte == null)
                    return NotFound("Reporte no encontrado.");

                var nombreBase = $"{reporte.TipoReporte}_{reporte.FechaGeneracion:yyyyMMdd_HHmm}";
                return formato switch
                {
                    "csv" => File(_exportService.ExportarCsv(reporte), "text/csv", $"{nombreBase}.csv"),
                    "excel" => File(_exportService.ExportarExcel(reporte), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{nombreBase}.xlsx"),
                    "pdf" => File(_exportService.ExportarPdf(reporte), "application/pdf", $"{nombreBase}.pdf"),
                    _ => BadRequest("Formato no soportado.")
                };
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al exportar el reporte.");
            }
        }

        // ---- CRUD original de registros de Reportes (se conserva para administración directa) ----

        [HttpGet("ObtenerReportes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerReportes()
        {
            try
            {
                var Reportes = await _ReportesRepository.ObtenerReportes();
                if (Reportes == null || !Reportes.Any())
                {
                    return NotFound("No se encontraron reportes.");
                }
                return Ok(Reportes);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error al obtener los reportes.");
            }

        }

        [HttpGet("ObtenerReportePorID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerReportePorId(Guid ID)
        {
            try
            {
                var reporte = await _ReportesRepository.ObtenerReportesPorId(ID);
                if (reporte == null)
                {
                    return NotFound("Reporte no encontrado.");
                }
                return Ok(reporte);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error al obtener el reporte.");
            }
        }

        [HttpDelete("EliminarReporte")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EliminarReporte(Guid ID)
        {
            try
            {
                var resultado = await _ReportesRepository.EliminarReporte(ID);
                if (!resultado)
                {
                    return BadRequest("No se pudo eliminar el reporte.");
                }
                return Ok("Reporte eliminado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error al eliminar el reporte.");
            }
        }
    }
}
