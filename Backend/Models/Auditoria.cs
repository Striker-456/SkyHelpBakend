using SkyHelp;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkyHelp.Models
{
    public class Auditoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IDLog { get; set; } = Guid.NewGuid();
        [Required]
        [ForeignKey("Usuarios")]
        public Guid IDUsuario { get; set; }
        [Required]
        [StringLength(50)]
        public required string TipoEvento { get; set; }
        [Required]
        [StringLength(50)]
        public required string TablaAfectada { get; set; }
        [Required]
        public Guid IDRegistro { get; set; } = Guid.NewGuid();
        [Required]
        [StringLength(300)]
        public required string Descripcion { get; set; }
        [Required]
        public DateTime FechaEvento { get; set; }
        [StringLength(45)]
        public string? DireccionIp { get; set; }
        [JsonIgnore]
        public virtual Usuarios? Usuario { get; set; }


    }
}
