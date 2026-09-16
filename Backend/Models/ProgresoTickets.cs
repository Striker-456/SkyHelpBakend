using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkyHelp.Models
{
    // Cada fila es una actualización del progreso del servicio registrada por el técnico
    // (o un administrador) sobre un ticket. La fila más reciente por IdTicket es el
    // "estado actual" mostrado tanto al técnico como al cliente; el resto conforma el
    // historial/trazabilidad pedido para el ticket.
    public class ProgresoTickets
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IdProgreso { get; set; } = Guid.NewGuid();
        [Required]
        public Guid IdTicket { get; set; }
        [Required]
        [Range(0, 100)]
        public int Porcentaje { get; set; }
        [Required]
        [StringLength(100)]
        public required string Etapa { get; set; }
        [StringLength(500)]
        public string? Descripcion { get; set; }
        [Required]
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        // Usuario técnico que hizo el registro (Tecnicos.IdTecnico). Nulo si fue un Administrador.
        public Guid? IdTecnico { get; set; }

        [JsonIgnore]
        public virtual Tickets? Ticket { get; set; }
        [JsonIgnore]
        public virtual Tecnicos? Tecnico { get; set; }
    }
}
