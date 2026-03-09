using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Interfaces;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Services
{
    public class BoletoService : IBoletoService
    {
        public Task<(bool success, string message)> ComprarMasivoAsync(BoletoMasivoDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Boleto?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Boleto>> GetByUsuarioAsync(int usuarioId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<int>> GetOcupadosAsync(int viajeId)
        {
            throw new NotImplementedException();
        }
    }
}
