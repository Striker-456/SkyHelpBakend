using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.DTOs.Estadisticas;
using SkyHelp.Models;
using SkyHelp.Services.Interfaces;

namespace SkyHelp.Services
{
    // Igual que ReportesService, agrega datos de Tickets/Tecnicos — consulta el
    // SkyHelpContext directamente en lugar de forzar métodos de agregación en cada repositorio.
    public class EstadisticasService : IEstadisticasService
    {
        private static readonly string[] MesesEs = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

        private readonly SkyHelpContext _context;

        public EstadisticasService(SkyHelpContext context)
        {
            _context = context;
        }

        public async Task<EstadisticasResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta)
        {
            var ahora = DateTime.UtcNow;
            var fin = hasta ?? ahora;
            var inicio = desde ?? ahora.AddMonths(-5).AddDays(1 - ahora.Day);

            // Sin "hasta" explícito no se recorta por límite superior: usar DateTime.UtcNow como tope
            // excluía tickets recién creados en cuanto había el mínimo desfase entre el reloj del
            // servidor y la hora de creación, mostrando menos tickets de los que realmente existen.
            var tickets = await _context.Tickets
                .Include(t => t.EstadoTicket)
                .Include(t => t.Tecnico).ThenInclude(tc => tc!.Usuario)
                .Where(t => t.FechaCreacion >= inicio && (hasta == null || t.FechaCreacion <= hasta))
                .ToListAsync();

            int ContarPorEstado(Func<string, bool> coincide) =>
                tickets.Count(t => t.EstadoTicket != null && coincide(t.EstadoTicket.NombreEstado));

            // FechaCierre solo existe para tickets cerrados después de agregar la columna; para no
            // subcontar tickets ya resueltos previamente, también se considera el nombre del estado.
            bool EsResuelto(Tickets t) => t.FechaCierre.HasValue ||
                (t.EstadoTicket != null &&
                    (t.EstadoTicket.NombreEstado.Contains("resuelto", StringComparison.OrdinalIgnoreCase) ||
                     t.EstadoTicket.NombreEstado.Contains("cerrado", StringComparison.OrdinalIgnoreCase)));

            var resueltos = tickets.Count(EsResuelto);
            var pendientes = ContarPorEstado(n => n.Contains("pendiente", StringComparison.OrdinalIgnoreCase));
            var enProgreso = ContarPorEstado(n => n.Contains("progreso", StringComparison.OrdinalIgnoreCase));
            var asignados = ContarPorEstado(n => n.Contains("asignado", StringComparison.OrdinalIgnoreCase));
            var usuariosActivos = tickets.Select(t => t.IdUsuario).Distinct().Count();
            var criticosAltos = tickets.Count(t =>
                t.Prioridad.Contains("crit", StringComparison.OrdinalIgnoreCase) ||
                t.Prioridad.Equals("Alta", StringComparison.OrdinalIgnoreCase));

            var resueltosConTiempo = tickets.Where(t => t.FechaCierre.HasValue && t.FechaCreacion.HasValue).ToList();
            double? tiempoPromedio = resueltosConTiempo.Count > 0
                ? resueltosConTiempo.Average(t => (t.FechaCierre!.Value - t.FechaCreacion!.Value).TotalHours)
                : null;

            var distribucion = tickets
                .GroupBy(t => t.EstadoTicket?.NombreEstado ?? "Sin estado")
                .Select(g => new DistribucionEstadoDto { Estado = g.Key, Total = g.Count() })
                .ToList();

            var porPrioridad = tickets
                .GroupBy(t => t.Prioridad)
                .Select(g => new TicketsPorPrioridadDto { Prioridad = g.Key, Total = g.Count() })
                .ToList();

            var meses = new List<ComparativaMensualDto>();
            var cursor = new DateTime(inicio.Year, inicio.Month, 1);
            var limite = new DateTime(fin.Year, fin.Month, 1);
            while (cursor <= limite && meses.Count < 24)
            {
                var mesActual = cursor;
                meses.Add(new ComparativaMensualDto
                {
                    Mes = MesesEs[mesActual.Month - 1],
                    Resueltos = tickets.Count(t => t.FechaCierre.HasValue && t.FechaCierre.Value.Year == mesActual.Year && t.FechaCierre.Value.Month == mesActual.Month),
                    Pendientes = tickets.Count(t => t.FechaCreacion.HasValue && t.FechaCreacion.Value.Year == mesActual.Year && t.FechaCreacion.Value.Month == mesActual.Month && !EsResuelto(t))
                });
                cursor = cursor.AddMonths(1);
            }

            var rendimiento = tickets
                .Where(t => t.IdTecnico != null)
                .GroupBy(t => t.IdTecnico!.Value)
                .Select(g => new RendimientoTecnicoDto
                {
                    Tecnico = g.First().Tecnico?.Usuario?.NombreCompleto ?? "Desconocido",
                    Total = g.Count(),
                    Resueltos = g.Count(EsResuelto),
                    Activos = g.Count(t => !EsResuelto(t)),
                    Efectividad = g.Count() > 0 ? Math.Round(g.Count(EsResuelto) * 100.0 / g.Count(), 0) : 0
                })
                .ToList();

            return new EstadisticasResumenDto
            {
                TotalTickets = tickets.Count,
                Resueltos = resueltos,
                Pendientes = pendientes,
                EnProgreso = enProgreso,
                Asignados = asignados,
                UsuariosActivos = usuariosActivos,
                CriticosAltos = criticosAltos,
                TiempoPromedioResolucionHoras = tiempoPromedio.HasValue ? Math.Round(tiempoPromedio.Value, 1) : null,
                DistribucionPorEstado = distribucion,
                TicketsPorPrioridad = porPrioridad,
                ComparativaMensual = meses,
                RendimientoPorTecnico = rendimiento
            };
        }
    }
}
