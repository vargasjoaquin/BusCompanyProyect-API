using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Asiento
    {
        [Key]
        public int AsientoID { get; set; }
        public int AutobusID { get; set; }
        public int NumeroAsiento { get; set; }
        public int Piso { get; set; } = 1;
        [Required, StringLength(20)]
        public string Ubicacion { get; set; } 

        [ForeignKey("AutobusID")]
        [JsonIgnore] 
        public virtual Autobus? Autobus { get; set; }
    }
}
