using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Dto
{
    public class RutaCreateDto
    {
        public string NombreRuta { get; set; }
        public int OrigenID { get; set; }
        public int DestinoID { get; set; }
        public decimal DistanciaKM { get; set; }

        public string? Origen { get; set; }  // AGREGADO
        public string? Destino { get; set; } // AGREGADO

        //public string? Descripcion { get; set; }
    }
}
