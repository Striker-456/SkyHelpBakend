namespace SkyHelp.DTOs.Reportes
{
    // Tipo: "tickets" | "usuarios" | "actividad" | "auditorias" | "rendimiento"
    public class GenerarReporteRequest
    {
        public string Tipo { get; set; } = string.Empty;
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? Estado { get; set; }
        public string? Categoria { get; set; }
    }
}
