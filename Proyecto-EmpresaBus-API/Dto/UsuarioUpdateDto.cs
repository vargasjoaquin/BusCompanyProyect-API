using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("password")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string? Password { get; set; }
        [StringLength(8, ErrorMessage = "El DNI debe tener máximo 8 caracteres.")]
        public string DNI { get; set; }

        public string? Direccion { get; set; }
        [StringLength(13, ErrorMessage = "El teléfono debe tener máximo 13 caracteres.")]
        public string? Telefono { get; set; }
        public string? Sexo { get; set; }
        public int? Edad { get; set; }
        public string? Provincia { get; set; }
        public string? Ciudad { get; set; }
        public int? ProvinciaID { get; set; }
    }
}