using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Request
{
    public class RegisterRequestDto
    {
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; }
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string DNI { get; set; }
        public string? Rol { get; set; }
        public string? Ciudad { get; set; }
        public int? ProvinciaID { get; set; }
        public int? LocalidadID { get; set; }
        public string? Direccion { get; set; }
        [StringLength(13, ErrorMessage = "El teléfono no puede superar los 13 caracteres.")]
        public string? Telefono { get; set; }

        public int? Edad { get; set; }
        public string? Sexo { get; set; }
    }
}
