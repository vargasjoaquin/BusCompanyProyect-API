using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Provincia
    {
        [Key]
        public int ProvinciaID { get; set; }
        [Required, StringLength(100)]
        public string NombreProvincia { get; set; }
    }
}
