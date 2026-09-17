using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkyHelp.Models
{
    public class EstadosTicket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IdEstado { get; set; } = Guid.NewGuid();
        [Required]
        public required string NombreEstado { get; set; }
        [Required]
        public required string Descripcion { get; set; }
        [JsonIgnore]
        public ICollection<Tickets>? Tickets { get; set; }



    }
}
