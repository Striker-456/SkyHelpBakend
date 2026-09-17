using System.Linq;

namespace SkyHelp.Models
{
    // Lista fija y ordenada de etapas del servicio técnico (progreso granular del ticket).
    // Debe coincidir EXACTAMENTE (mismo texto, mismo orden) con ETAPAS_SERVICIO en
    // Frontend/js/modules/tickets.js, ya que el front la usa para pintar la línea de tiempo
    // y este backend la usa para validar el campo Etapa de ProgresoTickets.
    public static class EtapasServicio
    {
        public static readonly string[] Orden = new[]
        {
            "Ticket recibido",
            "Equipo recibido",
            "Diagnóstico iniciado",
            "Inspección del equipo",
            "Identificación de la falla",
            "Pruebas de funcionamiento",
            "Diagnóstico finalizado",
            "En reparación",
            "Reparación finalizada",
            "Servicio finalizado"
        };

        public const string DiagnosticoIniciado = "Diagnóstico iniciado";
        public const string DiagnosticoFinalizado = "Diagnóstico finalizado";

        public static bool EsValida(string? etapa) =>
            !string.IsNullOrWhiteSpace(etapa) && Orden.Contains(etapa);
    }
}
