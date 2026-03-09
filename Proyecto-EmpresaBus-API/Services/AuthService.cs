using Proyecto_EmpresaBus_API.Interfaces;
using Proyecto_EmpresaBus_API.Request;

namespace Proyecto_EmpresaBus_API.Services
{
    public class AuthService : IAuthService
    {
        public Task<(bool success, string message)> ForgotPasswordAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<(bool success, string message, string token, int userId, string rol)> LoginAsync(LoginRequestDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<(bool success, string message)> RegisterAsync(RegisterRequestDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<(bool success, string message)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
