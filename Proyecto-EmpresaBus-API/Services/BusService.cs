using Proyecto_EmpresaBus_API.Interfaces;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Services
{
    public class BusService : IBusService
    {
        public Task<(bool success, string message, Autobus? bus)> CreateAsync(Autobus bus)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Autobus>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Autobus?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Autobus>> GetDeletedAsync()
        {
            throw new NotImplementedException();
        }

        public Task<(bool success, string message)> RestoreAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(int id, Autobus bus)
        {
            throw new NotImplementedException();
        }
    }
}
