using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;

namespace SkyHelp.Repositories
{
    public class ReportesRepository : IReportesRepository
    {
        private readonly SkyHelpContext _context;
        private readonly ILogger<ReportesRepository> _logger;
        public ReportesRepository(SkyHelpContext context, ILogger<ReportesRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Reportes>> ObtenerReportes()
        {
            return await _context.Reportes.ToListAsync();
        }
        public async Task<Reportes> ObtenerReportesPorId(Guid id)
        {
            return await _context.Reportes.FirstOrDefaultAsync(x => x.IdReporte == id);
        }

        public async Task<bool> EliminarReporte(Guid id)
        {
            try
            {
                var ReporteExistente = await _context.Reportes.FirstOrDefaultAsync(x => x.IdReporte == id);
                if (ReporteExistente == null)
                {
                    return false;
                }

                _context.Reportes.Remove(ReporteExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el reporte {IdReporte}", id);
                return false;
            }
        }

        public async Task<bool> ActualizarReporte(Reportes reportes)
        {
            try
            {
                var ReporteExistente = await _context.Reportes.FirstOrDefaultAsync(x => x.IdReporte == reportes.IdReporte);
                if (ReporteExistente == null)
                {
                    return false;
                }

                ReporteExistente.Titulo = reportes.Titulo;
                ReporteExistente.Descripcion = reportes.Descripcion;
                ReporteExistente.TipoReporte = reportes.TipoReporte;
                ReporteExistente.FechaGeneracion = reportes.FechaGeneracion;
                ReporteExistente.IdUsuario = reportes.IdUsuario;
                ReporteExistente.IdOrigen = reportes.IdOrigen;
                ReporteExistente.OrigenTabla = reportes.OrigenTabla;

                _context.Reportes.Update(ReporteExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el reporte {IdReporte}", reportes.IdReporte);
                return false;
            }
        }

        public async Task<bool> CrearReporte(Reportes reportes)
        {
            try
            {
                await _context.Reportes.AddAsync(reportes);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el reporte {TipoReporte}", reportes.TipoReporte);
                return false;
            }
        }
    }
}
