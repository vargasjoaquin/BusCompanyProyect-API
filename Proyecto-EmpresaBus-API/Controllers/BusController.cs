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

        /// <summary>
        /// Obtiene el listado de autobuses activos junto con su empresa asociada.
        /// </summary>
        /// <returns>Listado de autobuses.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Autobus>>> GetAutobuses()
        {
            return await _context.Autobuses.Include(a => a.Empresa).ToListAsync();
        }

        /// <summary>
        /// Obtiene el detalle de un autobús específico por su id.
        /// </summary>
        /// <param name="id">Id del autobús.</param>
        /// <returns>Autobús encontrado.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Autobus>> GetAutobus(int id)
        {
            var autobus = await _context.Autobuses
                .Include(a => a.Empresa)
                .FirstOrDefaultAsync(a => a.AutobusID == id);

            if (autobus == null) 
                return NotFound();

            return autobus;
        }

        /// <summary>
        /// Registra un nuevo autobús en el sistema.
        /// Normaliza datos, valida conflictos (patente e interno) y genera sus asientos.
        /// </summary>
        /// <param name="autobus">Entidad de autobús a crear.</param>
        /// <returns>Autobús creado.</returns>
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

            var conflictoPatente = await _context.Autobuses.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Matricula == autobus.Matricula);

            if (conflictoPatente != null)
            {
                string estado = conflictoPatente.IsDeleted ? "en la Papelera" : "en la lista de Activos";
                return BadRequest($"La patente '{autobus.Matricula}' ya existe {estado}.");
            }

            var conflictoNumero = await _context.Autobuses.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.NumeroBus == autobus.NumeroBus && a.EmpresaID == autobus.EmpresaID);

            if (conflictoNumero != null)
            {
                string estado = conflictoNumero.IsDeleted ? "en la Papelera" : "en la lista de Activos";
                return BadRequest($"El número de unidad '{autobus.NumeroBus}' ya existe para esta empresa {estado}.");
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

        /// <summary>
        /// Actualiza la información de un autobús existente.
        /// </summary>
        /// <param name="id">Id del autobús.</param>
        /// <param name="autobus">Datos actualizados.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAutobus(int id, Autobus autobus)
        {
            if (id != autobus.AutobusID) 
                return BadRequest();

            _context.Entry(autobus).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Autobuses.Any(e => e.AutobusID == id)) 
                    return NotFound();
                else 
                    throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Realiza la eliminación lógica de un autobús.
        /// Solo permitido para administradores y si no posee viajes futuros programados.
        /// </summary>
        /// <param name="id">Id del autobús.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteAutobus(int id)
        {
            var autobus = await _context.Autobuses.FindAsync(id);

            if (autobus == null) 
                return NotFound();

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

        /// <summary>
        /// Restaura un autobús previamente enviado a la papelera.
        /// Valida que no existan conflictos de patente o número interno.
        /// </summary>
        /// <param name="id">Id del autobús.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RestoreAutobus(int id)
        {
            var autobus = await _context.Autobuses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.AutobusID == id);

            if (autobus == null) 
                return NotFound("El autobús no existe (ni siquiera en la papelera).");

            if (!autobus.IsDeleted) 
                return BadRequest("El autobús ya está activo.");

            if (await _context.Autobuses.AnyAsync(a => a.Matricula == autobus.Matricula))
            {
                return BadRequest($"No se puede restaurar: la patente '{autobus.Matricula}' ya está en uso por otra unidad activa.");
            }

            if (await _context.Autobuses.AnyAsync(a => a.NumeroBus == autobus.NumeroBus && a.EmpresaID == autobus.EmpresaID))
            {
                return BadRequest($"No se puede restaurar: el interno #{autobus.NumeroBus} ya existe activo en esta empresa.");
            }

            autobus.IsDeleted = false;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Autobús restaurado correctamente." });
        }

        /// <summary>
        /// Obtiene el listado de autobuses eliminados.
        /// </summary>
        /// <returns>Listado de autobuses eliminados.</returns>
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

        /// <summary>
        /// Genera automáticamente los asientos físicos de un autobús según su capacidad.
        /// </summary>
        /// <param name="autobusId">Id del autobús.</param>
        /// <param name="capacidad">Cantidad total de asientos.</param>
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

        /// <summary>
        /// Calcula el próximo número interno disponible para una empresa.
        /// Considera también unidades eliminadas para evitar duplicados históricos.
        /// </summary>
        /// <param name="empresaId">Id de la empresa.</param>
        /// <returns>Próximo número interno sugerido.</returns>
        [HttpGet("next-internal/{empresaId}")]
        public async Task<ActionResult<int>> GetNextInternalNumber(int empresaId)
        {
            var buses = await _context.Autobuses
                .IgnoreQueryFilters()
                .Where(a => a.EmpresaID == empresaId)
                .ToListAsync();

            if (!buses.Any()) 
                return Ok(1); 

            int maxNumero = buses
                .Select(a => {
                    int.TryParse(a.NumeroBus, out int n);
                    return n;
                })
                .Max();

            return Ok(maxNumero + 1);
        }
    }
}

