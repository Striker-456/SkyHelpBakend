using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkyHelp.Models
{
    public class Reportes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IdReporte { get; set; } = Guid.NewGuid();
        [Required]
        [StringLength(50)]
        public required string Titulo { get; set; }
        [Required]
        [StringLength(500)]
        public required string Descripcion { get; set; }
        [Required]
        [StringLength(50)]
        public required string TipoReporte { get; set; }
        [Required]
        public DateTime? FechaGeneracion { get; set; } = DateTime.UtcNow;
        [Required]
        [ForeignKey("Usuario")]
        public Guid IdUsuario { get; set; }
        [Required]
        public Guid IdOrigen { get; set; } = Guid.NewGuid();
        [Required]  

        public required String OrigenTabla { get; set; }

        // Datos ya calculados del reporte (JSON de ReporteGeneradoDto), guardados para poder exportarlos
        // después sin tener que recalcularlos.
        [Column(TypeName = "text")]
        public string? Datos { get; set; }

        [JsonIgnore]
        public virtual Usuarios? Usuario { get; set; }


    }
}
