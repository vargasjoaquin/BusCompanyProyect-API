using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Parada
    {
        [Key]
        public int ParadaID { get; set; }

        [Required]
        [StringLength(150)]
        public string NombreParada { get; set; }

        public int LocalidadID { get; set; }

        [ForeignKey("LocalidadID")]
        public virtual Localidad? Localidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(9, 6)")]
        public decimal Latitud { get; set; }

        [Required]
        [Column(TypeName = "decimal(9, 6)")]
        public decimal Longitud { get; set; }
    }
}
