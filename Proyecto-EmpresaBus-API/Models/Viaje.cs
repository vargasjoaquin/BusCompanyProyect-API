using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Viaje
    {
        [Key]
        public int ViajeID { get; set; }

        public int? NumeroServicio { get; set; }

        public int RutaID { get; set; }
        public int AutobusID { get; set; }

        public DateTime FechaSalida { get; set; }
        public DateTime? FechaLlegadaEstimada { get; set; }


        [StringLength(20)]
        public string? Plataforma { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioBase { get; set; }

        public string EstadoViaje { get; set; } = "Programado";

        [ForeignKey("RutaID")]
        public virtual Ruta? Ruta { get; set; }

        [ForeignKey("AutobusID")]
        public virtual Autobus? Autobus { get; set; }

        public List<Boleto> Boletos { get; set; } = new List<Boleto>();

        public bool IsDeleted { get; set; } = false;
    }
}
