using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_EmpresaBus_API.Data;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Administrador")]
    public class BusController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public BusController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Autobus>>> GetAutobuses()
        {
            return await _context.Autobuses.Include(a => a.Empresa).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Autobus>> GetAutobus(int id)
        {
            var autobus = await _context.Autobuses
                .Include(a => a.Empresa)
                .FirstOrDefaultAsync(a => a.AutobusID == id);

            if (autobus == null) return NotFound();

            return autobus;
        }

        [HttpPost]
        public async Task<ActionResult<Autobus>> PostAutobus(Autobus autobus)
        {
            var dbName = _context.Database.GetDbConnection().Database;
            var serverName = _context.Database.GetDbConnection().DataSource;

            autobus.Matricula = autobus.Matricula?.Trim().ToUpper();
            autobus.Modelo = autobus.Modelo.Trim();

            var empresaExiste = await _context.Empresas.AnyAsync(e => e.EmpresaID == autobus.EmpresaID);
            if (!empresaExiste)
            {
                return BadRequest($"La empresa seleccionada no es válida.");
            }

            var yaExiste = await _context.Autobuses.AnyAsync(a => a.Matricula == autobus.Matricula);

            if (yaExiste)
            {
                Console.WriteLine($"[ERROR] ¡La API encontró que '{autobus.Matricula}' YA EXISTE en {dbName}!");
                return BadRequest($"La matrícula '{autobus.Matricula}' ya existe en la base de datos {dbName}.");
            }

            bool numeroExiste = await _context.Autobuses
                    .AnyAsync(a => a.NumeroBus == autobus.NumeroBus && !a.IsDeleted);

            if (numeroExiste)
            {
                return BadRequest($"El número de unidad '{autobus.NumeroBus}' ya existe en la flota.");
            }

            autobus.Empresa = null;
            autobus.Asientos = null;
            autobus.Viajes = null;

            try
            {
                _context.Autobuses.Add(autobus);
                await _context.SaveChangesAsync();

                await GenerarAsientos(autobus.AutobusID, autobus.CapacidadTotal);

                return CreatedAtAction(nameof(GetAutobus), new { id = autobus.AutobusID }, autobus);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAutobus(int id, Autobus autobus)
        {
            if (id != autobus.AutobusID) return BadRequest();

            _context.Entry(autobus).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Autobuses.Any(e => e.AutobusID == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteAutobus(int id)
        {
            var autobus = await _context.Autobuses.FindAsync(id);
            if (autobus == null) return NotFound();

            bool tieneViajesProgramados = await _context.Viajes
                .AnyAsync(v => v.AutobusID == id &&
                               v.FechaSalida > DateTime.UtcNow && 
                               !v.IsDeleted); 

            if (tieneViajesProgramados)
            {
                return BadRequest("No se puede eliminar: El autobús está asignado a viajes programados pendientes.");
            }

            autobus.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RestoreAutobus(int id)
        {
            var autobus = await _context.Autobuses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.AutobusID == id);

            if (autobus == null) return NotFound("El autobús no existe (ni siquiera en la papelera).");

            if (!autobus.IsDeleted) return BadRequest("El autobús ya está activo.");

            autobus.IsDeleted = false;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Autobús restaurado correctamente." });
        }

        [HttpGet("deleted")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<Autobus>>> GetDeletedAutobuses()
        {
            return await _context.Autobuses
                .IgnoreQueryFilters()
                .Where(a => a.IsDeleted)
                .Include(a => a.Empresa)
                .ToListAsync();
        }

        private async Task GenerarAsientos(int autobusId, int capacidad)
        {
            var listaAsientos = new List<Asiento>();

            for (int i = 1; i <= capacidad; i++)
            {
                listaAsientos.Add(new Asiento
                {
                    AutobusID = autobusId,
                    NumeroAsiento = i,
                    Ubicacion = (i % 4 == 0 || i % 4 == 1) ? "Ventana" : "Pasillo"
                });
            }

            _context.Asientos.AddRange(listaAsientos);
            await _context.SaveChangesAsync();
        }
    }
}

