namespace SkyHelp.DTOs.Pedidos
{
    public class ConfirmarEntregaResultado
    {
        public bool Exitoso { get; set; }
        public bool NoEncontrado { get; set; }
        public bool NoAutorizado { get; set; }
        public string? Mensaje { get; set; }
    }
}
