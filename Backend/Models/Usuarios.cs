using SkyHelp;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkyHelp.Models
{
    public class Usuarios
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IdUsuario { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey("Rol")]
        public Guid IdRol { get; set; }
        [Required]
        [StringLength(50)]
        public required string NombreUsuarios { get; set; }
        [Required]
        [StringLength(100)]
        public required string NombreCompleto { get; set; }
        [Required]
        [StringLength(50)]
        public required string Correo { get; set; }
        [Required]
        [StringLength(100)]
        public required string Contrasena { get; set; }
        [Required]
        [StringLength(50)]
        public required string EstadoCuenta { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }
        [JsonIgnore]
        public virtual Roles? Rol { get; set; }
        [JsonIgnore]

        public virtual ICollection<Auditoria>? Auditorias { get; set; }
        [JsonIgnore]
        public virtual ICollection<Domiciliarios>? Domiciliarios { get; set; }
        [JsonIgnore]
        public virtual ICollection<Reportes>? Reportes { get; set; }
        [JsonIgnore]
        public virtual ICollection<Tickets>? Tickets { get; set; }
        [JsonIgnore]
        public virtual ICollection<Pedidos>? Pedidos { get; set; }
        [JsonIgnore]
        public ICollection<Tecnicos>? Tecnico { get; set; }
    }
        
}
