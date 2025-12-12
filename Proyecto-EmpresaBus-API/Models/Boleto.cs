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
        public int AsientoID { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal PrecioFinal { get; set; } 

        public DateTime FechaCompra { get; set; }

        [StringLength(50)]
        public string EstadoBoleto { get; set; } 

        [ForeignKey("ViajeID")]
        public virtual Viaje? Viaje { get; set; }

        [ForeignKey("UsuarioID")]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey("AsientoID")]
        public virtual Asiento? Asiento { get; set; } 
    }
}
