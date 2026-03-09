using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Interfaces
{
    public interface IViajeService
    {
        Task<IEnumerable<Viaje>> GetAllAsync();
        Task<IEnumerable<Viaje>> BuscarAsync(string? o, string? d, DateTime? f, int? eId);
        Task<Viaje?> GetByIdAsync(int id);
        Task<(bool success, string message, Viaje? viaje)> CreateAsync(ViajeCreateDto dto);
        Task<(bool success, string message)> UpdateAsync(int id, ViajeCreateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> RestoreAsync(int id);
        Task<IEnumerable<Viaje>> GetDeletedAsync();
        Task<(bool success, string message)> LimpiarVencidosAsync();
    }
}
