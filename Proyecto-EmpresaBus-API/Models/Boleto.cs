using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Boleto
    {
        [Key]
        public int BoletoID { get; set; }

        public int ViajeID { get; set; }
        public int UsuarioID { get; set; }

        // CAMBIO IMPORTANTE: Ya no guardamos el número, sino el ID del asiento físico
        public int AsientoID { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioFinal { get; set; } // Antes era 'Precio'

        public DateTime FechaCompra { get; set; }

        [StringLength(50)]
        public string EstadoBoleto { get; set; } // Antes era 'Estado'

        [ForeignKey("ViajeID")]
        public virtual Viaje? Viaje { get; set; }

        [ForeignKey("UsuarioID")]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey("AsientoID")]
        public virtual Asiento? Asiento { get; set; } // Necesario para saber el número
    }
}
