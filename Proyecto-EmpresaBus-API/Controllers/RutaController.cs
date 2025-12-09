using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_EmpresaBus_API.Data;
using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RutaController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public RutaController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ruta>>> GetRutas()
        {
            return await _context.Rutas
                                 .Include(r => r.Origen)
                                 .Include(r => r.Destino)
                                 .Include(r => r.RutaParadas).ThenInclude(rp => rp.Parada)
                                 .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Ruta>> GetRuta(int id)
        {
            var ruta = await _context.Rutas
                                     .Include(r => r.Origen)
                                     .Include(r => r.Destino)
                                     .FirstOrDefaultAsync(r => r.RutaID == id);

            if (ruta == null) return NotFound();

            return ruta;
        }

        [HttpPost]
        public async Task<ActionResult<Ruta>> PostRuta(RutaCreateDto rutaDto)
        {
            // Lógica de compatibilidad: Buscar ID basado en el Nombre (String) del DTO
            var origen = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Origen);
            var destino = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Destino);

            if (origen == null || destino == null)
            {
                return BadRequest("El Origen o Destino especificado no existe en la base de datos de Localidades.");
            }

            var nuevaRuta = new Ruta
            {
                NombreRuta = rutaDto.NombreRuta,
                //Descripcion = rutaDto.Descripcion,
                // Precio removido de Ruta, ahora está en Viaje
                OrigenID = origen.LocalidadID,
                DestinoID = destino.LocalidadID
            };

            _context.Rutas.Add(nuevaRuta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRuta), new { id = nuevaRuta.RutaID }, nuevaRuta);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutRuta(int id, RutaCreateDto rutaDto)
        {
            var ruta = await _context.Rutas.FindAsync(id);
            if (ruta == null) return NotFound();

            // Buscar IDs nuevamente
            var origen = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Origen);
            var destino = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Destino);

            if (origen != null) ruta.OrigenID = origen.LocalidadID;
            if (destino != null) ruta.DestinoID = destino.LocalidadID;

            ruta.NombreRuta = rutaDto.NombreRuta;
            //ruta.Descripcion = rutaDto.Descripcion;

            _context.Entry(ruta).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Rutas.Any(r => r.RutaID == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRuta(int id)
        {
            var ruta = await _context.Rutas.FindAsync(id);
            if (ruta == null) return NotFound();

            _context.Rutas.Remove(ruta);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/itinerario")]
        public async Task<ActionResult<IEnumerable<object>>> GetItinerario(int id)
        {
            var paradas = await _context.RutaParadas
                .Where(rp => rp.RutaID == id)
                .OrderBy(rp => rp.Orden) // ¡Muy importante el orden!
                .Include(rp => rp.Parada) // Traer nombre y ubicación
                .Select(rp => new
                {
                    Orden = rp.Orden,
                    Nombre = rp.Parada.NombreParada,
                    Latitud = rp.Parada.Latitud,
                    Longitud = rp.Parada.Longitud,
                    LocalidadID = rp.Parada.LocalidadID
                })
                .ToListAsync();

            if (paradas == null || !paradas.Any()) return Ok(new List<object>());

            return Ok(paradas);
        }

        [HttpPost("{rutaId}/actualizar-itinerario")]
        public async Task<IActionResult> ActualizarItinerario(int rutaId, [FromBody] ItinerarioDto dto)
        {
            var ruta = await _context.Rutas.FindAsync(rutaId);
            if (ruta == null) return NotFound("Ruta no encontrada");

            // 1. Limpiar itinerario anterior (excepto origen y destino si quisieras, pero mejor limpiar todo y rehacer)
            var paradasAnteriores = _context.RutaParadas.Where(rp => rp.RutaID == rutaId);
            _context.RutaParadas.RemoveRange(paradasAnteriores);
            await _context.SaveChangesAsync();

            // 2. Crear nueva lista
            var nuevasParadasRuta = new List<RutaParada>();
            int orden = 1;

            // A. AGREGAR ORIGEN (Orden 1)
            var paradaOrigen = await GetOrCreateParada(ruta.OrigenID);
            nuevasParadasRuta.Add(new RutaParada { RutaID = rutaId, ParadaID = paradaOrigen.ParadaID, Orden = orden++ });

            // B. AGREGAR INTERMEDIAS
            if (dto.LocalidadesIds != null)
            {
                foreach (var locId in dto.LocalidadesIds)
                {
                    // Validar que no sea ni origen ni destino para no duplicar
                    if (locId != ruta.OrigenID && locId != ruta.DestinoID)
                    {
                        var paradaIntermedia = await GetOrCreateParada(locId);
                        nuevasParadasRuta.Add(new RutaParada { RutaID = rutaId, ParadaID = paradaIntermedia.ParadaID, Orden = orden++ });
                    }
                }
            }

            // C. AGREGAR DESTINO (Último Orden)
            var paradaDestino = await GetOrCreateParada(ruta.DestinoID);
            nuevasParadasRuta.Add(new RutaParada { RutaID = rutaId, ParadaID = paradaDestino.ParadaID, Orden = orden++ });

            // 3. Guardar
            _context.RutaParadas.AddRange(nuevasParadasRuta);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Itinerario actualizado con éxito", TotalParadas = orden - 1 });
        }

        // Método auxiliar para convertir Localidad -> Parada física
        private async Task<Parada> GetOrCreateParada(int localidadId)
        {
            // Buscamos si ya existe una parada principal para esa localidad
            var parada = await _context.Paradas.FirstOrDefaultAsync(p => p.LocalidadID == localidadId);

            if (parada == null)
            {
                // Si no existe, buscamos el nombre de la localidad y creamos una parada genérica
                var loc = await _context.Localidades.FindAsync(localidadId);
                parada = new Parada
                {
                    LocalidadID = localidadId,
                    NombreParada = "Terminal " + loc.NombreLocalidad,
                    Latitud = 0, // Deberías tener coords reales, pero por ahora 0
                    Longitud = 0
                };
                _context.Paradas.Add(parada);
                await _context.SaveChangesAsync();
            }
            return parada;
        }
    }
}
