using SkyHelp.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace SkyHelp
{
    public class Tecnicos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IdTecnico { get; set; } = Guid.NewGuid();
        [Required]
        public Guid IdUsuario { get; set; }
        [Required]
        public DateTime FechaRegistro { get; set; } //Estaba mal escrito como FechaResgistro
        [JsonIgnore]
        public Usuarios? Usuario { get; set; }
        [JsonIgnore]
        public ICollection<Tickets>? Tickets { get; set; }
    }
}
