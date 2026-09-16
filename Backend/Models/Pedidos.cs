using SkyHelp.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkyHelp
{
    public class Pedidos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IdPedido { get; set; } = Guid.NewGuid();
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NumeroPedido { get; set; }
        [Required]
        public Guid IdUsuario { get; set; }
        [Required]
        public Guid IdDomiciliario { get; set; }
        public DateTime? FechaPedido { get; set; } = DateTime.UtcNow;
        [Required]
        public required string DireccionEntrega { get; set; }
        [Required]
        public required string EstadoPedido { get; set; }

        public required string Observaciones  { get; set; }

        // Ticket de soporte que originó este pedido de entrega (si aplica).
        public Guid? IdTicket { get; set; }

        // Fecha y hora en que el domiciliario confirmó la entrega.
        public DateTime? FechaEntrega { get; set; }

        [JsonIgnore]
        public virtual Usuarios? Usuario { get; set; }
        [JsonIgnore]
        public virtual Domiciliarios? Domiciliario { get; set; }

    }
}
