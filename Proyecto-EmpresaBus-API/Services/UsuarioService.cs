using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Interfaces;
using Proyecto_EmpresaBus_API.Response;
using System.Security.Claims;

namespace Proyecto_EmpresaBus_API.Services
{
    public class UsuarioService : IUsuarioService
    {
        public Task<bool> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<UsuarioResponseDto?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UsuarioResponseDto>> GetDeletedAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UsuarioResponseDto>> GetListAsync(ClaimsPrincipal user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RestoreAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<(bool success, string message)> UpdateAsync(int id, UsuarioUpdateDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
