using Proyecto_EmpresaBus_API.Request;

namespace Proyecto_EmpresaBus_API.Interfaces
{
    public interface IAuthService
    {
        Task<(bool success, string message)> RegisterAsync(RegisterRequestDto dto);
        Task<(bool success, string message, string token, int userId, string rol)> LoginAsync(LoginRequestDto dto);
        Task<(bool success, string message)> ForgotPasswordAsync(string email);
        Task<(bool success, string message)> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
