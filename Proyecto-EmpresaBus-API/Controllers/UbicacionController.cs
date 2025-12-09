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

        [HttpGet("provincias")]
        public async Task<IActionResult> GetProvincias()
        {
            return Ok(await _context.Provincias.ToListAsync());
        }

        [HttpGet("localidades/{provinciaId}")]
        public async Task<IActionResult> GetLocalidades(int provinciaId)
        {
            var locs = await _context.Localidades
                .Where(l => l.ProvinciaID == provinciaId)
                .ToListAsync();
            return Ok(locs);
        }
    }
}
