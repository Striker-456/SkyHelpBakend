using SkyHelp.DTOs.Reportes;

namespace SkyHelp.Services.Interfaces
{
    public interface IReportesService
    {
        Task<ReporteGeneradoDto> GenerarAsync(GenerarReporteRequest request, Guid idUsuario);
        Task<List<ReporteRecienteDto>> ObtenerRecientesAsync();
        Task<ReporteGeneradoDto?> ObtenerGeneradoPorIdAsync(Guid idReporte);
    }
}
