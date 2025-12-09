using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Dto
{
    public class BoletoCreateDto
    {
        [Required] public int ViajeID { get; set; }
        [Required] public int UsuarioID { get; set; }
        [Required] public int AsientoID { get; set; }

        [Required]
        public int NumeroAsiento { get; set; }
    }
}
