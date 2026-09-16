using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SkyHelp.Models
{ 
    public class Tickets
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid IdTicket {  get ; set; } = Guid.NewGuid();
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NumeroTicket { get; set; }
        [Required]
        [StringLength(200)]
        public required string Descripcion { get; set; }
        [Required]
        [StringLength(50)]
        public required string Categoria { get; set; }
        [Required]
        [StringLength(50)]
        public required string Prioridad { get; set; }
        [Required]
        public DateTime? FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaCierre { get; set; }
        [StringLength(500)]
        public string? Diagnostico { get; set; }
        public DateTime? FechaDiagnostico { get; set; }
        [StringLength(300)]
        public string? FallaEncontrada { get; set; }
        [StringLength(300)]
        public string? PruebasRealizadas { get; set; }
        [StringLength(500)]
        public string? Observaciones { get; set; }
        [StringLength(500)]
        public string? Recomendaciones { get; set; }
        // Dirección de entrega que el cliente indica al crear el ticket (opcional). La usa
        // PedidosService al generar el Pedido de entrega, en vez del placeholder genérico.
        [StringLength(200)]
        public string? DireccionEntrega { get; set; }
        [Required]
        public Guid IdEstado { get; set; }
        [Required]
        [ForeignKey("Usuario")]
        public Guid IdUsuario { get; set; }
        [ForeignKey("Domiciliario")]
        public Guid? IdDomiciliario { get; set; }
        [ForeignKey("Tecnico")]
        public Guid? IdTecnico { get; set; }
        [JsonIgnore]
        public virtual Usuarios? Usuario { get; set; }
        [JsonIgnore]
        public virtual Domiciliarios? Domiciliario { get; set; }
        [JsonIgnore]
        public virtual Tecnicos? Tecnico { get; set; }
        [JsonIgnore]
        public virtual EstadosTicket? EstadoTicket { get; set; }
        [JsonIgnore]
        public virtual ICollection<ProgresoTickets>? ProgresoTickets { get; set; }
    }
}
