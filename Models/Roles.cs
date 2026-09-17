using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkyHelp.Models
{
    public class Roles
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]// Llave primaria auto-generada
        public Guid IDRol { get; set; } = Guid.NewGuid(); // Llave primaria
        [Required]
        [StringLength(50)]  
        public required string NombreRol { get; set; }   
        [StringLength(200)]
        public required string Descripcion { get; set; }
        [JsonIgnore]
        public ICollection<Usuarios>? Usuario { get; set; }// Relación uno a muchos con Usuarios
    }
}
