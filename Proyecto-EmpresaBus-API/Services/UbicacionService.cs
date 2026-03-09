using Proyecto_EmpresaBus_API.Interfaces;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Services
{
    public class UbicacionService : IUbicacionService
    {
        public Task<IEnumerable<Localidad>> GetLocalidadesAsync(int provId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Provincia>> GetProvinciasAsync()
        {
            throw new NotImplementedException();
        }
    }
}
