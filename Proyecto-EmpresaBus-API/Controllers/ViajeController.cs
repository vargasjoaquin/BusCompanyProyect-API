using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_EmpresaBus_API.Data;
using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Interfaces;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ViajeController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IEmailService _emailService;

        public ViajeController(ApiDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
                        .Include(v => v.Ruta).ThenInclude(r => r.Origen).ThenInclude(o => o.Provincia)
                        .Include(v => v.Ruta).ThenInclude(r => r.Destino).ThenInclude(d => d.Provincia)
                        .Include(v => v.Autobus).ThenInclude(a => a.Empresa)
                        .Include(v => v.Boletos)
                        .AsQueryable();

            if (!string.IsNullOrWhiteSpace(origen))
            {
                var filtro = origen.Trim().ToLower();
                query = query.Where(v => v.Ruta.Origen.NombreLocalidad.ToLower().Contains(filtro) ||
                                         v.Ruta.Origen.Provincia.NombreProvincia.ToLower().Contains(filtro));
            }

            if (!string.IsNullOrWhiteSpace(destino))
            {
                var filtro = destino.Trim().ToLower();
                query = query.Where(v => v.Ruta.Destino.NombreLocalidad.ToLower().Contains(filtro) ||
                                         v.Ruta.Destino.Provincia.NombreProvincia.ToLower().Contains(filtro));
            }

            if (fecha.HasValue) query = query.Where(v => v.FechaSalida.Date == fecha.Value.Date);
            if (empresaId.HasValue && empresaId > 0) query = query.Where(v => v.Autobus.EmpresaID == empresaId.Value);

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

            var autobus = await _context.Autobuses.Include(a => a.Empresa)
                                .FirstOrDefaultAsync(a => a.AutobusID == viajeDto.AutobusID);
            if (autobus == null) return BadRequest("El AutobusID no existe.");

            var ultimoServicio = await _context.Viajes
                                        .Where(v => v.Autobus.EmpresaID == autobus.EmpresaID)
                                        .MaxAsync(v => (int?)v.NumeroServicio) ?? 0;

            DateTime fechaSalida = viajeDto.FechaViaje.Date + viajeDto.HoraSalida.TimeOfDay;

            DateTime inicioRango = fechaSalida.AddMinutes(-20);
            DateTime finRango = fechaSalida.AddMinutes(5);

            bool plataformaOcupada = await _context.Viajes
                .AnyAsync(v => v.Plataforma == viajeDto.Plataforma &&
                               v.FechaSalida >= inicioRango &&
                               v.FechaSalida <= finRango &&
                               !v.IsDeleted);

            if (plataformaOcupada)
            {
                return BadRequest($"Conflicto detectado: La plataforma {viajeDto.Plataforma} ya estará en uso en el rango de {inicioRango:HH:mm} a {finRango:HH:mm}.");
            }

            double distancia = (double)ruta.DistanciaKM;
            if (distancia <= 0) distancia = 100;

            double tiempoDeViajeHoras = (distancia / 80.0) + 0.5;
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
                IsDeleted = false,
                NumeroServicio = ultimoServicio + 1
            };

            _context.Viajes.Add(nuevoViaje);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetViaje), new { id = nuevoViaje.ViajeID }, nuevoViaje);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutViaje(int id, ViajeCreateDto viajeDto)
        {
            var viaje = await _context.Viajes.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.ViajeID == id);
            if (viaje == null) return NotFound("El viaje no existe.");

            var ruta = await _context.Rutas.FindAsync(viajeDto.RutaID);
            if (ruta == null) return BadRequest("La RutaID no es válida.");

            DateTime nuevaFechaSalida = viajeDto.FechaViaje.Date + viajeDto.HoraSalida.TimeOfDay;
            bool plataformaChocada = await _context.Viajes.AnyAsync(v =>
                v.Plataforma == viajeDto.Plataforma &&
                v.FechaSalida == nuevaFechaSalida &&
                v.ViajeID != id &&
                !v.IsDeleted);

            if (plataformaChocada)
                return BadRequest($"La plataforma {viajeDto.Plataforma} ya está ocupada en ese horario.");

            double distancia = (double)ruta.DistanciaKM;
            if (distancia <= 0) distancia = 100;
            double horasViaje = (distancia / 80.0) + 0.5;

            viaje.RutaID = viajeDto.RutaID;
            viaje.AutobusID = viajeDto.AutobusID;
            viaje.FechaSalida = nuevaFechaSalida;
            viaje.FechaLlegadaEstimada = nuevaFechaSalida.AddHours(horasViaje);
            viaje.PrecioBase = viajeDto.PrecioBase;
            viaje.Plataforma = viajeDto.Plataforma;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteViaje(int id)
        {
            var viaje = await _context.Viajes
        .Include(v => v.Ruta)
        .Include(v => v.Boletos).ThenInclude(b => b.Usuario)
        .FirstOrDefaultAsync(v => v.ViajeID == id);

            if (viaje == null) return NotFound();

            if (viaje.Boletos.Any())
            {
                var correosYPersonas = viaje.Boletos.Select(b => b.Usuario).Distinct().ToList();

                foreach (var user in correosYPersonas)
                {
                    string alertHtml = $@"
                <div style='font-family: Arial; padding: 25px; border: 1px solid #dc3545; border-radius: 15px; max-width: 550px;'>
                    <h2 style='color: #dc3545;'>Servicio Cancelado ⚠️</h2>
                    <p>Hola <strong>{user.NombreCompleto}</strong>,</p>
                    <p>Te informamos que el viaje <strong>{viaje.Ruta.NombreRuta}</strong> programado para el <strong>{viaje.FechaSalida:dd/MM/yyyy HH:mm}</strong> ha sido suspendido.</p>
                    <p>Tus pasajes han sido guardados en tu historial como <strong>'Cancelado por Empresa'</strong>. Por favor comunícate con soporte para el reintegro.</p>
                </div>";

                    _ = Task.Run(async () => {
                        await _emailService.SendEmailAsync(user.Email, "IMPORTANTE: Viaje Cancelado - Bux App", alertHtml, true);
                    });
                }
            }

            viaje.IsDeleted = true; // Aplicamos soft delete
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("LimpiarVencidos")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> LimpiarVencidos()
        {
            var ahora = DateTime.Now;
            var viajesVencidos = await _context.Viajes
                                        .Where(v => !v.IsDeleted && v.FechaSalida < ahora)
                                        .ToListAsync();

            if (viajesVencidos.Count == 0)
            {
                return Ok(new { Message = "No se encontraron viajes vencidos para limpiar." });
            }

            foreach (var viaje in viajesVencidos)
            {
                viaje.IsDeleted = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Se han movido {viajesVencidos.Count} viajes vencidos a la papelera." });
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
                    .ThenInclude(a => a.Empresa)
                .ToListAsync();
        }

    }
}
