using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Dto
{
    public class UsuarioUpdateDto
    {
        public int UsuarioID { get; set; }

        [Required]
        [StringLength(150)]
        public string NombreCompleto { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        public string Rol { get; set; }
        public string? Password { get; set; }
    
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Sexo { get; set; }
        public int? Edad { get; set; }
        public string? Provincia { get; set; }
        public string? Ciudad { get; set; }


    }
}
