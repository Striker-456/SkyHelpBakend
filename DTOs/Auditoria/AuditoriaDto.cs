namespace SkyHelp.DTOs.Auditoria
{
    public class AuditoriaDto
    {
        public Guid Id { get; set; }
        public DateTime FechaEvento { get; set; }
        public Guid IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string TipoEvento { get; set; } = string.Empty;
        public string TablaAfectada { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public Guid IdRegistro { get; set; }
        public string? DireccionIp { get; set; }
    }
}
