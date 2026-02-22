using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Proyecto_EmpresaBus_API.Dto
{
    public class UsuarioUpdateDto
    {
        [JsonPropertyName("UsuarioID")]
        public int UsuarioID { get; set; }

        [JsonPropertyName("nombreCompleto")]
        public string NombreCompleto { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("rol")]
        public string Rol { get; set; }

        [JsonPropertyName("Password")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string? Password { get; set; }

        [JsonPropertyName("dni")]
        [StringLength(8, ErrorMessage = "El DNI debe tener máximo 8 caracteres.")]
        public string DNI { get; set; }

        [JsonPropertyName("direccion")]
        public string? Direccion { get; set; }

        [JsonPropertyName("telefono")]
        [StringLength(13, ErrorMessage = "El teléfono debe tener máximo 13 caracteres.")]
        public string? Telefono { get; set; }

        [JsonPropertyName("sexo")]
        public string? Sexo { get; set; }

        [JsonPropertyName("edad")]
        public int? Edad { get; set; }

        [JsonPropertyName("ciudad")]
        public string? Ciudad { get; set; }

        [JsonPropertyName("provinciaID")]
        public int? ProvinciaID { get; set; }
    }
}