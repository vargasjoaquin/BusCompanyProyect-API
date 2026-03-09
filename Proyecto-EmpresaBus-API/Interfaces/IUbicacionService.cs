using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Interfaces
{
    public interface IUbicacionService
    {
        Task<IEnumerable<Provincia>> GetProvinciasAsync();
        Task<IEnumerable<Localidad>> GetLocalidadesAsync(int provId);
    }
}
