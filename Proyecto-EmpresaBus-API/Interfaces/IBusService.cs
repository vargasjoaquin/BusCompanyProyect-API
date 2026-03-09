using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Interfaces
{
    public interface IBusService
    {
        Task<IEnumerable<Autobus>> GetAllAsync();
        Task<Autobus?> GetByIdAsync(int id);
        Task<(bool success, string message, Autobus? bus)> CreateAsync(Autobus bus);
        Task<bool> UpdateAsync(int id, Autobus bus);
        Task<bool> DeleteAsync(int id);
        Task<(bool success, string message)> RestoreAsync(int id);
        Task<IEnumerable<Autobus>> GetDeletedAsync();
    }
}
