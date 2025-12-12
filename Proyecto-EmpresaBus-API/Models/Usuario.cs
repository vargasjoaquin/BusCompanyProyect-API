using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_EmpresaBus_API.Models
{
    public class Usuario
    {

        [Key]
        public int UsuarioID { get; set; }
        [Required, StringLength(150)]
        public string NombreCompleto { get; set; }
        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        [Required, StringLength(50)]
        public string Rol { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }

        public int? LocalidadID { get; set; }
        [ForeignKey("LocalidadID")]
        public virtual Localidad? Localidad { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public int? Edad { get; set; }
        [StringLength(20)]
        public string? Sexo { get; set; }

    }
}
