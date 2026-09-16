namespace SkyHelp.DTOs.Pedidos
{
    public class AsignarDomiciliarioResultado
    {
        public bool Exitoso { get; set; }
        public bool NoEncontrado { get; set; }
        public bool DiagnosticoPendiente { get; set; }
        public bool DomiciliarioInvalido { get; set; }
        public string? Mensaje { get; set; }
    }
}
