using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Interfaces;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Services
{
    public class ViajeService : IViajeService
    {
        public Task<IEnumerable<Viaje>> BuscarAsync(string? o, string? d, DateTime? f, int? eId)
        {
            throw new NotImplementedException();
        }

        public Task<(bool success, string message, Viaje? viaje)> CreateAsync(ViajeCreateDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Viaje>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Viaje?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Viaje>> GetDeletedAsync()
        {
            throw new NotImplementedException();
        }

        public Task<(bool success, string message)> LimpiarVencidosAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> RestoreAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<(bool success, string message)> UpdateAsync(int id, ViajeCreateDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
