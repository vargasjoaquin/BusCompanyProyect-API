using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Interfaces
{
    public interface IBoletoService
    {
        Task<IEnumerable<int>> GetOcupadosAsync(int viajeId);
        Task<IEnumerable<Boleto>> GetByUsuarioAsync(int usuarioId);
        Task<Boleto?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id); 
        Task<(bool success, string message)> ComprarMasivoAsync(BoletoMasivoDto dto);
    }
}
