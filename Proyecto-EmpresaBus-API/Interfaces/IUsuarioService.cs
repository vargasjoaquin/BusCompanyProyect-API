using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Response;
using System.Security.Claims;

namespace Proyecto_EmpresaBus_API.Interfaces
{
    public interface IUsuarioService
    {
        Task<IEnumerable<UsuarioResponseDto>> GetListAsync(ClaimsPrincipal user);
        Task<UsuarioResponseDto?> GetByIdAsync(int id);
        Task<(bool success, string message)> UpdateAsync(int id, UsuarioUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> RestoreAsync(int id);
        Task<IEnumerable<UsuarioResponseDto>> GetDeletedAsync();
    }
}
