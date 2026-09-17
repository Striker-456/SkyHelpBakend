namespace SkyHelp.DTOs.Estadisticas
{
    public class DistribucionEstadoDto
    {
        public string Estado { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    public class TicketsPorPrioridadDto
    {
        public string Prioridad { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    public class ComparativaMensualDto
    {
        public string Mes { get; set; } = string.Empty;
        public int Resueltos { get; set; }
        public int Pendientes { get; set; }
    }

    public class RendimientoTecnicoDto
    {
        public string Tecnico { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Resueltos { get; set; }
        public int Activos { get; set; }
        public double Efectividad { get; set; }
    }

    public class EstadisticasResumenDto
    {
        public int TotalTickets { get; set; }
        public int Resueltos { get; set; }
        public int Pendientes { get; set; }
        public int EnProgreso { get; set; }
        public int Asignados { get; set; }
        public int UsuariosActivos { get; set; }
        public int CriticosAltos { get; set; }
        public double? TiempoPromedioResolucionHoras { get; set; }
        public List<DistribucionEstadoDto> DistribucionPorEstado { get; set; } = new();
        public List<TicketsPorPrioridadDto> TicketsPorPrioridad { get; set; } = new();
        public List<ComparativaMensualDto> ComparativaMensual { get; set; } = new();
        public List<RendimientoTecnicoDto> RendimientoPorTecnico { get; set; } = new();
    }
}
