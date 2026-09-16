using SkyHelp.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkyHelp
{
    public class Domiciliarios
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IdDomiciliario { get; set; }
        [Required]
        [StringLength(50)]
        public required string NombreCompleto { get; set; }
        [Required]
        [StringLength(50)]
        public required string Telefono { get; set; }
        [Required]
        [StringLength(100)]
        public required string Email { get; set; }
        [Required]
        public required string EstadoActividad { get; set; }
        public Guid IDUsuario { get; set; }
        [JsonIgnore]
        public virtual Usuarios? Usuario { get; set; }
        [JsonIgnore]
        public virtual ICollection<Pedidos>? Pedidos { get; set; }
        [JsonIgnore]
        public virtual ICollection<Tickets>? Tickets { get; set; }
    }
}
