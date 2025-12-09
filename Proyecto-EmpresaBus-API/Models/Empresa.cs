using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Empresa
    {
        [Key]
        public int EmpresaID { get; set; }
        [Required, StringLength(150)]
        public string NombreEmpresa { get; set; }
        [Required, StringLength(50)]
        public string? Direccion { get; set; }
        public string? TelefonoContacto { get; set; }
        public string? EmailContacto { get; set; }
    }
}
