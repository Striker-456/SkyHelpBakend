namespace SkyHelp.DTOs.Reportes
{
    public class ReporteGeneradoDto
    {
        public Guid IdReporte { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string TipoReporte { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public List<string> Columnas { get; set; } = new();
        public List<Dictionary<string, object?>> Filas { get; set; } = new();
        public Dictionary<string, object?> Resumen { get; set; } = new();
    }

    public class ReporteRecienteDto
    {
        public Guid IdReporte { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string TipoReporte { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public string GeneradoPor { get; set; } = string.Empty;
    }
}
