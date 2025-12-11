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
                                 .Include(v => v.Autobus)
                                 .ToListAsync();
        }

        [HttpGet("Buscar")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Viaje>>> BuscarViajes(
                                                                        [FromQuery] string? origen,
                                                                        [FromQuery] string? destino,
                                                                        [FromQuery] DateTime? fecha)
        {
            var query = _context.Viajes
                                .Include(v => v.Ruta).ThenInclude(r => r.Origen)
                                .Include(v => v.Ruta).ThenInclude(r => r.Destino)
                                .Include(v => v.Autobus)
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

            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Viaje>> GetViaje(int id)
        {
            var viaje = await _context.Viajes
                                      .Include(v => v.Ruta).ThenInclude(r => r.Origen)
                                      .Include(v => v.Ruta).ThenInclude(r => r.Destino)
                                      .Include(v => v.Autobus)
                                      .FirstOrDefaultAsync(v => v.ViajeID == id);

            if (viaje == null) return NotFound();

            return viaje;
        }

        [HttpPost]
        public async Task<ActionResult<Viaje>> PostViaje(ViajeCreateDto viajeDto)
        {
            // 1. Validaciones existentes...
            var ruta = await _context.Rutas.FirstOrDefaultAsync(r => r.RutaID == viajeDto.RutaID);
            if (ruta == null) return BadRequest("La RutaID no existe.");

            var autobusExiste = await _context.Autobuses.AnyAsync(a => a.AutobusID == viajeDto.AutobusID);
            if (!autobusExiste) return BadRequest("El AutobusID no existe.");

            // 2. LÓGICA DE CÁLCULO DE TIEMPO REAL
            // Velocidad promedio estimada (puedes ajustarla, ej: 80 o 90 km/h)
            double velocidadPromedioKmH = 90.0;

            // Obtenemos distancia. Si es 0 o null, asumimos 1 hora por defecto para no romper
            double distancia = (double)ruta.DistanciaKM;
            if (distancia <= 0) distancia = 90; // Fallback

            double horasDuracion = distancia / velocidadPromedioKmH;

            // Calculamos Fecha Salida combinada
            DateTime fechaSalida = viajeDto.FechaViaje.Date + viajeDto.HoraSalida.TimeOfDay;

            // Calculamos Fecha Llegada (Salida + Duracion)
            DateTime fechaLlegada = fechaSalida.AddHours(horasDuracion);

            var nuevoViaje = new Viaje
            {
                RutaID = viajeDto.RutaID,
                AutobusID = viajeDto.AutobusID,
                FechaSalida = fechaSalida,
                FechaLlegadaEstimada = fechaLlegada, // <--- GUARDAMOS EL CÁLCULO
                PrecioBase = viajeDto.PrecioBase, // Asegúrate de tener esto en tu DTO
                EstadoViaje = "Programado",
                Plataforma = viajeDto.Plataforma
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

            // VALIDACIÓN: Chequear si hay boletos vendidos
            // Boletos no suele tener SoftDelete, así que AnyAsync directo funciona bien.
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

            viaje.IsDeleted = false; // Restaurar
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("deleted")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<Viaje>>> GetDeletedViajes()
        {
            // IMPORTANTE: IgnoreQueryFilters() es vital para ver los IsDeleted=true
            return await _context.Viajes
                .IgnoreQueryFilters()
                .Where(v => v.IsDeleted)
                .Include(v => v.Ruta)
                .Include(v => v.Autobus)
                .ToListAsync();
        }

    }
}
