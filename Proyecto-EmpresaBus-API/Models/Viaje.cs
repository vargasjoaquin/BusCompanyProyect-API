using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Viaje
    {
        [Key]
        public int ViajeID { get; set; }

        public int RutaID { get; set; }
        public int AutobusID { get; set; }

        // En la nueva BBDD unimos Fecha y Hora en una sola columna
        public DateTime FechaSalida { get; set; }
        public DateTime? FechaLlegadaEstimada { get; set; }

        // El precio ahora está en el Viaje (no en la ruta), permitiendo precios dinámicos
        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioBase { get; set; }

        public string EstadoViaje { get; set; } = "Programado";

        [ForeignKey("RutaID")]
        public virtual Ruta? Ruta { get; set; }

        [ForeignKey("AutobusID")]
        public virtual Autobus? Autobus { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
