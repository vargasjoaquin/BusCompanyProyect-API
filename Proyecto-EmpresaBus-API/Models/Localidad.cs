using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Localidad
    {
        [Key]
        public int LocalidadID { get; set; }
        [Required, StringLength(100)]
        public string NombreLocalidad { get; set; }
        [StringLength(20)]
        public string? CodigoPostal { get; set; }

        public int ProvinciaID { get; set; }
        [ForeignKey("ProvinciaID")]
        public virtual Provincia? Provincia { get; set; }
    }
}
