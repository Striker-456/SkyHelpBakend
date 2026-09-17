using SkyHelp.DTOs.Reportes;

namespace SkyHelp.Services.Interfaces
{
    public interface IReporteExportService
    {
        byte[] ExportarCsv(ReporteGeneradoDto reporte);
        byte[] ExportarExcel(ReporteGeneradoDto reporte);
        byte[] ExportarPdf(ReporteGeneradoDto reporte);
    }
}
