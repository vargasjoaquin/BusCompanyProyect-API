namespace Proyecto_EmpresaBus_API.Request
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        public string Token { get; set; }
        public string Password { get; set; }
    }
}
