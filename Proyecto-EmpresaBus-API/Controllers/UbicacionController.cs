using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_EmpresaBus_API.Data;

namespace Proyecto_EmpresaBus_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UbicacionController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public UbicacionController(ApiDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el listado completo de provincias registradas en el sistema.
        /// </summary>
        /// <returns>Lista de provincias.</returns>
        [HttpGet("provincias")]
        public async Task<IActionResult> GetProvincias()
        {
            return Ok(await _context.Provincias.ToListAsync());
        }

        /// <summary>
        /// Obtiene las localidades asociadas a una provincia específica.
        /// </summary>
        /// <param name="provinciaId">Id de la provincia.</param>
        /// <returns>Lista de localidades correspondientes a la provincia indicada.</returns>
        [HttpGet("localidades/{provinciaId}")]
        public async Task<IActionResult> GetLocalidades(int provinciaId)
        {
            var localidades = await _context.Localidades
                .Where(l => l.ProvinciaID == provinciaId)
                .ToListAsync();

            return Ok(localidades);
        }
    }
}
