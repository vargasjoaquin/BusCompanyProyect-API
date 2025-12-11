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
    [Authorize]
    public class BoletoController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public BoletoController(ApiDbContext context)
        {
            _context = context;
        }

        // POST: api/Boleto
        [HttpPost]
        public async Task<ActionResult<Boleto>> ComprarBoleto(BoletoCreateDto boletoDto)
        {
            var viaje = await _context.Viajes
                .Include(v => v.Ruta)
                .FirstOrDefaultAsync(v => v.ViajeID == boletoDto.ViajeID);

            if (viaje == null) return NotFound("Viaje no existe");

            var asientoFisico = await _context.Asientos
                .FirstOrDefaultAsync(a => a.AutobusID == viaje.AutobusID && a.NumeroAsiento == boletoDto.NumeroAsiento);

            if (asientoFisico == null)
            {
                return BadRequest($"El asiento {boletoDto.NumeroAsiento} no existe en la configuración del autobús.");
            }

            bool asientoOcupado = await _context.Boletos.AnyAsync(b =>
                b.ViajeID == boletoDto.ViajeID &&
                b.AsientoID == asientoFisico.AsientoID);

            if (asientoOcupado)
            {
                return BadRequest($"El asiento {boletoDto.NumeroAsiento} ya está ocupado.");
            }

            var nuevoBoleto = new Boleto
            {
                ViajeID = boletoDto.ViajeID,
                UsuarioID = boletoDto.UsuarioID,
                AsientoID = asientoFisico.AsientoID,
                FechaCompra = DateTime.UtcNow,
                EstadoBoleto = "Confirmado",
                PrecioFinal = viaje.PrecioBase
            };

            _context.Boletos.Add(nuevoBoleto);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBoleto", new { id = nuevoBoleto.BoletoID }, nuevoBoleto);
        }

        // GET: api/Boleto/Ocupados/5
        [HttpGet("Ocupados/{viajeId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<int>>> GetOcupados(int viajeId)
        {
            var ocupados = await _context.Boletos
                .Include(b => b.Asiento)
                .Where(b => b.ViajeID == viajeId)
                .Select(b => b.Asiento.NumeroAsiento)
                .ToListAsync();

            return Ok(ocupados);
        }

        // GET: api/Boleto
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Boleto>>> GetBoletos([FromQuery] int? usuarioId)
        {
            var query = _context.Boletos
                .Include(b => b.Viaje).ThenInclude(v => v.Ruta).ThenInclude(r => r.Origen)
                .Include(b => b.Viaje).ThenInclude(v => v.Ruta).ThenInclude(r => r.Destino)
                .Include(b => b.Viaje).ThenInclude(v => v.Autobus).ThenInclude(a => a.Empresa)
                .Include(b => b.Asiento)
                .Include(b => b.Usuario)

                .AsQueryable();

            if (usuarioId.HasValue)
            {
                query = query.Where(b => b.UsuarioID == usuarioId.Value);
            }

            var lista = await query.OrderByDescending(b => b.FechaCompra).ToListAsync();

            return Ok(lista);
        }

        // GET: api/Boleto/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Boleto>> GetBoleto(int id)
        {
            var boleto = await _context.Boletos
                .Include(b => b.Viaje).ThenInclude(v => v.Ruta)
                .Include(b => b.Asiento)
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.BoletoID == id);

            if (boleto == null) return NotFound();

            return boleto;
        }

        // DELETE: api/Boleto/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBoleto(int id)
        {
            var boleto = await _context.Boletos.FindAsync(id);
            if (boleto == null)
            {
                return NotFound("El boleto no existe.");
            }

            _context.Boletos.Remove(boleto);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Boleto/comprar-masivo
        [HttpPost("comprar-masivo")]
        public async Task<IActionResult> ComprarBoletosMasivos(BoletoMasivoDto dto)
        {
            if (dto.Asientos == null || !dto.Asientos.Any())
                return BadRequest("Debe seleccionar al menos un asiento.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var viaje = await _context.Viajes
                    .Include(v => v.Ruta)
                    .FirstOrDefaultAsync(v => v.ViajeID == dto.ViajeID);

                if (viaje == null) return NotFound("Viaje no encontrado");

                var asientosFisicos = await _context.Asientos
                    .Where(a => a.AutobusID == viaje.AutobusID && dto.Asientos.Contains(a.NumeroAsiento))
                    .ToListAsync();

                if (asientosFisicos.Count != dto.Asientos.Count)
                    return BadRequest("Uno o más asientos solicitados no existen en este bus.");

                var idsAsientos = asientosFisicos.Select(a => a.AsientoID).ToList();

                var ocupados = await _context.Boletos
                    .Where(b => b.ViajeID == dto.ViajeID && idsAsientos.Contains(b.AsientoID))
                    .Include(b => b.Asiento)
                    .ToListAsync();

                if (ocupados.Any())
                {
                    return BadRequest($"Ya están ocupados: {string.Join(", ", ocupados.Select(o => o.Asiento.NumeroAsiento))}");
                }

                var nuevosBoletos = new List<Boleto>();

                foreach (var asiento in asientosFisicos)
                {
                    nuevosBoletos.Add(new Boleto
                    {
                        ViajeID = dto.ViajeID,
                        UsuarioID = dto.UsuarioID,
                        AsientoID = asiento.AsientoID,
                        FechaCompra = DateTime.UtcNow,
                        EstadoBoleto = "Confirmado",
                        PrecioFinal = viaje.PrecioBase
                    });
                }

                _context.Boletos.AddRange(nuevosBoletos);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { Message = $"Se compraron {nuevosBoletos.Count} boletos exitosamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
