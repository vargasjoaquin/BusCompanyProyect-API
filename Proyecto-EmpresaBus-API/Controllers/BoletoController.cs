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
    [Authorize]
    public class BoletoController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IEmailService _emailService;

        public BoletoController(ApiDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // POST: api/Boleto
        [HttpPost]
        public async Task<ActionResult<Boleto>> ComprarBoleto(BoletoCreateDto boletoDto)
        {
            var viaje = await _context.Viajes.FindAsync(boletoDto.ViajeID);
            if (viaje == null) return NotFound("El viaje no existe.");

            var asientoFisico = await _context.Asientos
                .FirstOrDefaultAsync(a => a.AutobusID == viaje.AutobusID && a.NumeroAsiento == boletoDto.NumeroAsiento);

            if (asientoFisico == null) return BadRequest($"El asiento {boletoDto.NumeroAsiento} no existe en este bus.");

            // CORRECCIÓN: Solo bloquear si está "Confirmado"
            bool asientoOcupado = await _context.Boletos.AnyAsync(b =>
                b.ViajeID == boletoDto.ViajeID &&
                b.AsientoID == asientoFisico.AsientoID &&
                b.EstadoBoleto == "Confirmado");

            if (asientoOcupado) return BadRequest($"El asiento {boletoDto.NumeroAsiento} ya está ocupado.");

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
                    .Where(b => b.ViajeID == viajeId && b.EstadoBoleto == "Confirmado")
                    .Select(b => b.Asiento.NumeroAsiento)
                    .ToListAsync();

            return Ok(ocupados);
        }

        // GET: api/Boleto
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Boleto>>> GetBoletos([FromQuery] int? usuarioId)
        {
            var query = _context.Boletos
                .IgnoreQueryFilters()
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
                .IgnoreQueryFilters()
                .Include(b => b.Viaje)
                    .ThenInclude(v => v.Ruta)
                        .ThenInclude(r => r.Origen)
                .Include(b => b.Viaje)
                    .ThenInclude(v => v.Ruta)
                        .ThenInclude(r => r.Destino)
                .Include(b => b.Viaje)
                    .ThenInclude(v => v.Autobus)
                        .ThenInclude(a => a.Empresa)
                .Include(b => b.Asiento)
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.BoletoID == id);

            if (boleto == null) return NotFound("El boleto no existe.");

            return boleto;
        }

        // DELETE: api/Boleto/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBoleto(int id)
        {
            var boleto = await _context.Boletos
        .Include(b => b.Viaje).ThenInclude(v => v.Ruta)
        .Include(b => b.Usuario)
        .FirstOrDefaultAsync(b => b.BoletoID == id);

            if (boleto == null) return NotFound();

            string mailUsuario = boleto.Usuario.Email;
            string nombre = boleto.Usuario.NombreCompleto;
            string rutaInfo = boleto.Viaje.Ruta.NombreRuta;

            boleto.EstadoBoleto = "Cancelado por el Usuario";
            await _context.SaveChangesAsync();

            string cuerpoBaja = $@"
        <div style='font-family: Arial; padding: 25px; border: 1px solid #888; border-radius: 15px;'>
            <h3 style='color: #444;'>Cancelación de Pasaje Confirmada</h3>
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Confirmamos que el pasaje para el viaje <strong>{rutaInfo}</strong> ha sido cancelado con éxito y el asiento ya fue liberado.</p>
            <p>Esperamos viajar con vos nuevamente pronto.</p>
        </div>";

            _ = Task.Run(async () => {
                await _emailService.SendEmailAsync(mailUsuario, "Baja de Reserva - Bux App", cuerpoBaja, true);
            });

            return NoContent();
        }

        // POST: api/Boleto/comprar-masivo
        [HttpPost("comprar-masivo")]
        public async Task<IActionResult> ComprarBoletosMasivos(BoletoMasivoDto dto)
        {
            if (dto.Asientos == null || !dto.Asientos.Any())
                return BadRequest("Debe seleccionar al menos un asiento.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var viaje = await _context.Viajes.Include(v => v.Ruta).FirstOrDefaultAsync(v => v.ViajeID == dto.ViajeID);
                    if (viaje == null) return NotFound("Viaje no encontrado");

                    var asientosFisicos = await _context.Asientos
                        .Where(a => a.AutobusID == viaje.AutobusID && dto.Asientos.Contains(a.NumeroAsiento))
                        .ToListAsync();

                    if (asientosFisicos.Any(a => a.AsientoID == 0))
                    {
                        return BadRequest("Error de integridad: Los asientos en la base de datos no tienen un ID válido.");
                    }

                    if (asientosFisicos.Count != dto.Asientos.Count)
                        return BadRequest("Uno o más asientos solicitados no existen.");

                    var asientoIds = asientosFisicos.Select(a => a.AsientoID).ToList();

                    var boletosActivos = await _context.Boletos
                        .AnyAsync(b => b.ViajeID == dto.ViajeID &&
                                       asientoIds.Contains(b.AsientoID) &&
                                       b.EstadoBoleto == "Confirmado");

                    if (boletosActivos)
                    {
                        return BadRequest("Uno o más asientos acaban de ser ocupados. Por favor, intente con otros.");
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

                    try
                    {
                        var datosFull = await _context.Viajes
                            .Include(v => v.Ruta).ThenInclude(r => r.Origen)
                            .Include(v => v.Ruta).ThenInclude(r => r.Destino)
                            .Include(v => v.Autobus).ThenInclude(a => a.Empresa)
                            .FirstOrDefaultAsync(v => v.ViajeID == dto.ViajeID);

                        var usuario = await _context.Usuarios.FindAsync(dto.UsuarioID);

                        if (datosFull != null && usuario != null)
                        {
                            decimal total = datosFull.PrecioBase * dto.Asientos.Count;
                            byte[] pdfBytes = Proyecto_EmpresaBus_API.Helpers.PdfGenerator.GenerarTicketPdf(datosFull, usuario, dto.Asientos, total);

                            string mensajeStyled = $@"
                        <div style='font-family: Arial; padding: 20px; border: 1px solid #ddd; border-radius: 15px; max-width: 500px;'>
                            <h2 style='color: #4CAF50;'>¡Confirmación de Viaje! 🚌</h2>
                            <p>Hola <strong>{usuario.NombreCompleto}</strong>,</p>
                            <p>Tu compra ha sido procesada con éxito. Aquí tienes tu itinerario:</p>
                            <ul style='list-style: none; padding: 0;'>
                                <li><strong>Ruta:</strong> {datosFull.Ruta.NombreRuta}</li>
                                <li><strong>Fecha:</strong> {datosFull.FechaSalida:dd/MM/yyyy HH:mm} hs</li>
                                <li><strong>Asientos:</strong> {string.Join(", ", dto.Asientos)}</li>
                            </ul>
                            <p>Hemos adjuntado tu ticket PDF a este correo.</p>
                            <br><p>Gracias por elegir <strong>Bux App</strong>.</p>
                        </div>";

                            _ = Task.Run(async () => {
                                try
                                {
                                    string nombreLimpio = usuario.NombreCompleto.Replace(" ", "_");
                                    string nombreTicket = $"Ticket-Bux-{nombreLimpio}.pdf";

                                    await _emailService.SendEmailAsync(
                                        usuario.Email,
                                        "¡Viaje Confirmado! - Ticket Adjunto",
                                        mensajeStyled,
                                        true,
                                        pdfBytes,
                                        nombreTicket
                                    );
                                }
                                catch { }
                            });
                        }
                    }
                    catch { }

                    return Ok(new { Message = $"Se compraron {nuevosBoletos.Count} boletos exitosamente." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var mensajeError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return StatusCode(500, $"Error real en BD: {mensajeError}");
                }
            });
        }
    }
}
