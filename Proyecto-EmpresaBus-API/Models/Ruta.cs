using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Ruta
    {
        [Key]
        public int RutaID { get; set; }

        [Required, StringLength(100)]
        public string NombreRuta { get; set; }

        // El precio se eliminó de aquí porque ahora está en Viaje
        // public decimal Precio { get; set; } <--- ELIMINADO

        //public string? Descripcion { get; set; }

        // Ahora usamos IDs para origen y destino
        public int OrigenID { get; set; }
        public int DestinoID { get; set; }

        [ForeignKey("OrigenID")]
        public virtual Localidad? Origen { get; set; }
        [ForeignKey("DestinoID")]
        public virtual Localidad? Destino { get; set; }

        public virtual ICollection<RutaParada> RutaParadas { get; set; } = new List<RutaParada>();

        public decimal DistanciaKM { get; set; }
    }
}
