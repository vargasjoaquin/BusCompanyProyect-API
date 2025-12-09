using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_EmpresaBus_API.Dto
{
    public class ViajeCreateDto
    {
        [Required]
        public int RutaID { get; set; }

        [Required]
        public int AutobusID { get; set; }

        [Required]
        public DateTime FechaViaje { get; set; }

        [Required]
        public DateTime HoraSalida { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioBase { get; set; }
    }
}
