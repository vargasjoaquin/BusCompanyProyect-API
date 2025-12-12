using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Autobus
    {
        [Key]
        public int AutobusID { get; set; }

        public int EmpresaID { get; set; } 

        [Required, StringLength(20)]
        public string Matricula { get; set; }

        [Required]
        public string NumeroBus { get; set; }

        public string? Modelo { get; set; }
        public int CapacidadTotal { get; set; }

        [ForeignKey("EmpresaID")]
        public virtual Empresa? Empresa { get; set; }

        [JsonIgnore]
        public virtual ICollection<Asiento> Asientos { get; set; } = new List<Asiento>();

        [JsonIgnore]
        public virtual ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();

        public bool IsDeleted { get; set; } = false;
    }
}
