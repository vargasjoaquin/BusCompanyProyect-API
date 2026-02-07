using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_EmpresaBus_API.Data;
using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ViajeController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public ViajeController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Viaje>>> GetViajes()
        {
            return await _context.Viajes
                                 .Include(v => v.Ruta).ThenInclude(r => r.Origen)
                                 .Include(v => v.Ruta).ThenInclude(r => r.Destino)
                                 .Include(v => v.Autobus).ThenInclude(a => a.Empresa)
                                 .Include(v => v.Boletos)
                                 .ToListAsync();
        }

        [HttpGet("Buscar")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Viaje>>> BuscarViajes(
                                                                        [FromQuery] string? origen,
                                                                        [FromQuery] string? destino,
                                                                        [FromQuery] DateTime? fecha,
                                                                        [FromQuery] int? empresaId)
        {
            var query = _context.Viajes
                                .Include(v => v.Ruta).ThenInclude(r => r.Origen)
                                .Include(v => v.Ruta).ThenInclude(r => r.Destino)
                                .Include(v => v.Autobus).ThenInclude(a => a.Empresa)
                                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(origen))
            {
                var origenBuscado = origen.Trim().ToLower();
                query = query.Where(v => v.Ruta.Origen.NombreLocalidad.ToLower().Contains(origenBuscado));
            }

            if (!string.IsNullOrWhiteSpace(destino))
            {
                var destinoBuscado = destino.Trim().ToLower();
                query = query.Where(v => v.Ruta.Destino.NombreLocalidad.ToLower().Contains(destinoBuscado));
            }

            if (fecha.HasValue)
            {
                var f = fecha.Value.Date;
                query = query.Where(v => v.FechaSalida.Date == f);
            }

            // Filtrar por Empresa (Mejora solicitada)
            if (empresaId.HasValue && empresaId > 0)
            {
                query = query.Where(v => v.Autobus.EmpresaID == empresaId.Value);
            }

            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Viaje>> GetViaje(int id)
        {
            var viaje = await _context.Viajes
                .Include(v => v.Ruta).ThenInclude(r => r.Origen)
                .Include(v => v.Ruta).ThenInclude(r => r.Destino)
                .Include(v => v.Autobus).ThenInclude(a => a.Empresa)
                .FirstOrDefaultAsync(v => v.ViajeID == id);

            if (viaje == null) return NotFound();

            return viaje;
        }

        [HttpPost]
        public async Task<ActionResult<Viaje>> PostViaje(ViajeCreateDto viajeDto)
        {
            var ruta = await _context.Rutas.FirstOrDefaultAsync(r => r.RutaID == viajeDto.RutaID);
            if (ruta == null) return BadRequest("La RutaID no existe.");

            var autobus = await _context.Autobuses.FirstOrDefaultAsync(a => a.AutobusID == viajeDto.AutobusID);
            if (autobus == null) return BadRequest("El AutobusID no existe.");
            double distancia = (double)ruta.DistanciaKM;
            if (distancia <= 0) distancia = 100; 

            double tiempoDeViajeHoras = (distancia / 80.0) + 0.5;
            DateTime fechaSalida = viajeDto.FechaViaje.Date + viajeDto.HoraSalida.TimeOfDay;
            DateTime fechaLlegada = fechaSalida.AddHours(tiempoDeViajeHoras);

            var nuevoViaje = new Viaje
            {
                RutaID = viajeDto.RutaID,
                AutobusID = viajeDto.AutobusID,
                FechaSalida = fechaSalida,
                FechaLlegadaEstimada = fechaLlegada,
                PrecioBase = viajeDto.PrecioBase,
                EstadoViaje = "Programado",
                Plataforma = viajeDto.Plataforma,
                IsDeleted = false
            };

            _context.Viajes.Add(nuevoViaje);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetViaje), new { id = nuevoViaje.ViajeID }, nuevoViaje);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutViaje(int id, ViajeCreateDto viajeDto)
        {
            var viaje = await _context.Viajes.FindAsync(id);
            if (viaje == null) return NotFound();

            viaje.RutaID = viajeDto.RutaID;
            viaje.AutobusID = viajeDto.AutobusID;
            viaje.FechaSalida = viajeDto.FechaViaje.Date + viajeDto.HoraSalida.TimeOfDay;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteViaje(int id)
        {
            var viaje = await _context.Viajes.FindAsync(id);
            if (viaje == null) return NotFound();

            bool tienePasajeros = await _context.Boletos.AnyAsync(b => b.ViajeID == id);

            if (tienePasajeros)
            {
                return BadRequest("No se puede eliminar el viaje: Ya hay pasajes vendidos a clientes.");
            }

            viaje.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreViaje(int id)
        {
            var viaje = await _context.Viajes.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.ViajeID == id);
            if (viaje == null) return NotFound();

            viaje.IsDeleted = false; 
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("deleted")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<Viaje>>> GetDeletedViajes()
        {
            return await _context.Viajes
                .IgnoreQueryFilters()
                .Where(v => v.IsDeleted)
                .Include(v => v.Ruta)
                .Include(v => v.Autobus)
                .ToListAsync();
        }

    }
}
