using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.DTOs.Reportes;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;
using SkyHelp.Services.Interfaces;
using System.Text.Json;

namespace SkyHelp.Services
{
    // Los reportes agregan datos de varias tablas (Tickets, Usuarios, Auditoria) — por eso este
    // servicio consulta directamente el SkyHelpContext en modo lectura, en vez de forzar métodos
    // de agregación específicos de reportería dentro de cada repositorio de entidad.
    public class ReportesService : IReportesService
    {
        private readonly SkyHelpContext _context;
        private readonly IReportesRepository _reportesRepository;

        public ReportesService(SkyHelpContext context, IReportesRepository reportesRepository)
        {
            _context = context;
            _reportesRepository = reportesRepository;
        }

        public async Task<ReporteGeneradoDto> GenerarAsync(GenerarReporteRequest request, Guid idUsuario)
        {
            var dto = request.Tipo?.Trim().ToLowerInvariant() switch
            {
                "tickets" => await GenerarReporteTickets(request),
                "usuarios" => await GenerarReporteUsuarios(request),
                "actividad" => await GenerarReporteActividad(request, soloOperacionesCriticas: false),
                "auditorias" => await GenerarReporteActividad(request, soloOperacionesCriticas: true),
                "rendimiento" => await GenerarReporteRendimiento(request),
                _ => throw new ArgumentException($"Tipo de reporte no soportado: {request.Tipo}")
            };

            var entidad = new Reportes
            {
                IdReporte = dto.IdReporte,
                Titulo = dto.Titulo,
                Descripcion = $"Reporte generado con {dto.Filas.Count} registro(s).",
                TipoReporte = dto.TipoReporte,
                FechaGeneracion = dto.FechaGeneracion,
                IdUsuario = idUsuario,
                IdOrigen = Guid.NewGuid(),
                OrigenTabla = dto.TipoReporte,
                Datos = JsonSerializer.Serialize(dto)
            };
            await _reportesRepository.CrearReporte(entidad);

            return dto;
        }

        public async Task<List<ReporteRecienteDto>> ObtenerRecientesAsync()
        {
            var reportes = await _reportesRepository.ObtenerReportes();
            var idsUsuarios = reportes.Select(r => r.IdUsuario).Distinct().ToList();
            var usuarios = await _context.Usuarios
                .Where(u => idsUsuarios.Contains(u.IdUsuario))
                .ToDictionaryAsync(u => u.IdUsuario, u => u.NombreCompleto);

            return reportes
                .OrderByDescending(r => r.FechaGeneracion)
                .Take(20)
                .Select(r => new ReporteRecienteDto
                {
                    IdReporte = r.IdReporte,
                    Titulo = r.Titulo,
                    TipoReporte = r.TipoReporte,
                    FechaGeneracion = r.FechaGeneracion ?? DateTime.MinValue,
                    GeneradoPor = usuarios.TryGetValue(r.IdUsuario, out var nombre) ? nombre : "Desconocido"
                })
                .ToList();
        }

        public async Task<ReporteGeneradoDto?> ObtenerGeneradoPorIdAsync(Guid idReporte)
        {
            var reporte = await _reportesRepository.ObtenerReportesPorId(idReporte);
            if (reporte == null || string.IsNullOrWhiteSpace(reporte.Datos))
                return null;
            return JsonSerializer.Deserialize<ReporteGeneradoDto>(reporte.Datos);
        }

        private async Task<ReporteGeneradoDto> GenerarReporteTickets(GenerarReporteRequest request)
        {
            var query = _context.Tickets
                .Include(t => t.EstadoTicket)
                .Include(t => t.Tecnico).ThenInclude(tc => tc!.Usuario)
                .Include(t => t.Usuario)
                .AsQueryable();

            if (request.Desde.HasValue) query = query.Where(t => t.FechaCreacion >= request.Desde.Value);
            if (request.Hasta.HasValue) query = query.Where(t => t.FechaCreacion <= request.Hasta.Value);
            if (!string.IsNullOrWhiteSpace(request.Categoria)) query = query.Where(t => t.Categoria == request.Categoria);
            if (!string.IsNullOrWhiteSpace(request.Estado)) query = query.Where(t => t.EstadoTicket != null && t.EstadoTicket.NombreEstado == request.Estado);

            var tickets = await query.OrderByDescending(t => t.FechaCreacion).ToListAsync();

            var filas = tickets.Select(t => new Dictionary<string, object?>
            {
                ["Numero"] = t.NumeroTicket,
                ["Descripcion"] = t.Descripcion,
                ["Categoria"] = t.Categoria,
                ["Prioridad"] = t.Prioridad,
                ["Estado"] = t.EstadoTicket?.NombreEstado ?? "",
                ["Cliente"] = t.Usuario?.NombreCompleto ?? "",
                ["Tecnico"] = t.Tecnico?.Usuario?.NombreCompleto ?? "Sin asignar",
                ["FechaCreacion"] = t.FechaCreacion,
                ["FechaCierre"] = t.FechaCierre
            }).ToList();

            var resumen = new Dictionary<string, object?>
            {
                ["Total"] = tickets.Count,
                ["Resueltos"] = tickets.Count(EsResuelto),
                ["Pendientes"] = tickets.Count(t => !EsResuelto(t))
            };

            return NuevoReporte("Reporte de Tickets", "tickets",
                new List<string> { "Numero", "Descripcion", "Categoria", "Prioridad", "Estado", "Cliente", "Tecnico", "FechaCreacion", "FechaCierre" },
                filas, resumen);
        }

        private async Task<ReporteGeneradoDto> GenerarReporteUsuarios(GenerarReporteRequest request)
        {
            var usuarios = await _context.Usuarios.Include(u => u.Rol).ToListAsync();
            var conteoTickets = await _context.Tickets
                .GroupBy(t => t.IdUsuario)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToListAsync();
            var mapaConteo = conteoTickets.ToDictionary(x => x.Key, x => x.Total);

            var filas = usuarios.Select(u => new Dictionary<string, object?>
            {
                ["Nombre"] = u.NombreCompleto,
                ["Correo"] = u.Correo,
                ["Rol"] = u.Rol?.NombreRol ?? "",
                ["Estado"] = u.EstadoCuenta,
                ["TicketsCreados"] = mapaConteo.TryGetValue(u.IdUsuario, out var c) ? c : 0
            }).ToList();

            var resumen = new Dictionary<string, object?> { ["Total"] = usuarios.Count };

            return NuevoReporte("Reporte de Usuarios", "usuarios",
                new List<string> { "Nombre", "Correo", "Rol", "Estado", "TicketsCreados" }, filas, resumen);
        }

        private async Task<ReporteGeneradoDto> GenerarReporteActividad(GenerarReporteRequest request, bool soloOperacionesCriticas)
        {
            var query = _context.Auditoria.Include(a => a.Usuario).AsQueryable();
            if (request.Desde.HasValue) query = query.Where(a => a.FechaEvento >= request.Desde.Value);
            if (request.Hasta.HasValue) query = query.Where(a => a.FechaEvento <= request.Hasta.Value);
            if (soloOperacionesCriticas)
                query = query.Where(a => a.TipoEvento == "Crear" || a.TipoEvento == "Actualizar" || a.TipoEvento == "Eliminar");

            var registros = await query.OrderByDescending(a => a.FechaEvento).ToListAsync();

            var filas = registros.Select(a => new Dictionary<string, object?>
            {
                ["Fecha"] = a.FechaEvento,
                ["Usuario"] = a.Usuario?.NombreCompleto ?? "",
                ["Accion"] = a.TipoEvento,
                ["Modulo"] = a.TablaAfectada,
                ["Descripcion"] = a.Descripcion
            }).ToList();

            var resumen = new Dictionary<string, object?> { ["Total"] = registros.Count };

            var titulo = soloOperacionesCriticas ? "Reporte de Auditorías" : "Reporte de Actividad";
            var tipo = soloOperacionesCriticas ? "auditorias" : "actividad";
            return NuevoReporte(titulo, tipo, new List<string> { "Fecha", "Usuario", "Accion", "Modulo", "Descripcion" }, filas, resumen);
        }

        private async Task<ReporteGeneradoDto> GenerarReporteRendimiento(GenerarReporteRequest request)
        {
            var query = _context.Tickets
                .Include(t => t.Tecnico).ThenInclude(tc => tc!.Usuario)
                .Include(t => t.EstadoTicket)
                .Where(t => t.IdTecnico != null)
                .AsQueryable();
            if (request.Desde.HasValue) query = query.Where(t => t.FechaCreacion >= request.Desde.Value);
            if (request.Hasta.HasValue) query = query.Where(t => t.FechaCreacion <= request.Hasta.Value);

            var tickets = await query.ToListAsync();

            var porTecnico = tickets
                .GroupBy(t => t.IdTecnico!.Value)
                .Select(g => new
                {
                    Nombre = g.First().Tecnico?.Usuario?.NombreCompleto ?? "Desconocido",
                    Total = g.Count(),
                    Resueltos = g.Count(EsResuelto),
                    Activos = g.Count(t => !EsResuelto(t)),
                    TiempoPromedioHoras = g.Where(t => t.FechaCierre.HasValue)
                        .Select(t => (t.FechaCierre!.Value - (t.FechaCreacion ?? t.FechaCierre.Value)).TotalHours)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .ToList();

            var filas = porTecnico.Select(p => new Dictionary<string, object?>
            {
                ["Tecnico"] = p.Nombre,
                ["Total"] = p.Total,
                ["Resueltos"] = p.Resueltos,
                ["Activos"] = p.Activos,
                ["Efectividad"] = p.Total > 0 ? Math.Round(p.Resueltos * 100.0 / p.Total, 0) : 0,
                ["TiempoPromedioHoras"] = Math.Round(p.TiempoPromedioHoras, 1)
            }).ToList();

            var resumen = new Dictionary<string, object?> { ["TecnicosConActividad"] = porTecnico.Count };

            return NuevoReporte("Reporte de Rendimiento", "rendimiento",
                new List<string> { "Tecnico", "Total", "Resueltos", "Activos", "Efectividad", "TiempoPromedioHoras" }, filas, resumen);
        }

        // FechaCierre solo existe para tickets cerrados después de agregar la columna; para no
        // subcontar tickets ya resueltos previamente, también se considera el nombre del estado.
        private static bool EsResuelto(Tickets t) => t.FechaCierre.HasValue ||
            (t.EstadoTicket != null &&
                (t.EstadoTicket.NombreEstado.Contains("resuelto", StringComparison.OrdinalIgnoreCase) ||
                 t.EstadoTicket.NombreEstado.Contains("cerrado", StringComparison.OrdinalIgnoreCase)));

        private static ReporteGeneradoDto NuevoReporte(string titulo, string tipo, List<string> columnas, List<Dictionary<string, object?>> filas, Dictionary<string, object?> resumen) => new()
        {
            IdReporte = Guid.NewGuid(),
            Titulo = titulo,
            TipoReporte = tipo,
            FechaGeneracion = DateTime.UtcNow,
            Columnas = columnas,
            Filas = filas,
            Resumen = resumen
        };
    }
}
