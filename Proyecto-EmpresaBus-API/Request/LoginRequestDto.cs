using System.ComponentModel.DataAnnotations;

namespace Proyecto_EmpresaBus_API.Request
{
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
