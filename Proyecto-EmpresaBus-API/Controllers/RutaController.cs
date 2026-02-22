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
            var origen = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Origen);
            var destino = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Destino);

            if (origen == null || destino == null)
            {
                return BadRequest("El Origen o Destino especificado no existe en la base de datos de Localidades.");
            }

            double lat1 = (double)origen.Latitud;
            double lon1 = (double)origen.Longitud;
            double lat2 = (double)destino.Latitud;
            double lon2 = (double)destino.Longitud;

            decimal distanciaCalculada = (decimal)CalcularDistanciaHaversine(lat1, lon1, lat2, lon2);

            var nuevaRuta = new Ruta
            {
                NombreRuta = rutaDto.NombreRuta,
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

            var origen = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Origen);
            var destino = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Destino);

            if (origen != null) ruta.OrigenID = origen.LocalidadID;
            if (destino != null) ruta.DestinoID = destino.LocalidadID;

            ruta.NombreRuta = rutaDto.NombreRuta;

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
                .OrderBy(rp => rp.Orden) 
                .Include(rp => rp.Parada) 
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
            var ruta = await _context.Rutas
                .Include(r => r.Origen)
                .Include(r => r.Destino)
                .FirstOrDefaultAsync(r => r.RutaID == rutaId);

            if (ruta == null) return NotFound("Ruta no encontrada");

            var paradasAnteriores = _context.RutaParadas.Where(rp => rp.RutaID == rutaId);
            _context.RutaParadas.RemoveRange(paradasAnteriores);
            await _context.SaveChangesAsync();

            var nuevasParadasRuta = new List<RutaParada>();
            var puntosGeograficos = new List<Localidad>();
            int orden = 1;

            puntosGeograficos.Add(ruta.Origen);
            var pOrigen = await GetOrCreateParada(ruta.OrigenID);
            nuevasParadasRuta.Add(new RutaParada { RutaID = rutaId, ParadaID = pOrigen.ParadaID, Orden = orden++ });

            if (dto.LocalidadesIds != null && dto.LocalidadesIds.Any())
            {
                foreach (var locId in dto.LocalidadesIds)
                {
                    var loc = await _context.Localidades.FindAsync(locId);
                    if (loc != null && locId != ruta.OrigenID && locId != ruta.DestinoID)
                    {
                        puntosGeograficos.Add(loc);
                        var pInt = await GetOrCreateParada(locId);
                        nuevasParadasRuta.Add(new RutaParada { RutaID = rutaId, ParadaID = pInt.ParadaID, Orden = orden++ });
                    }
                }
            }

            puntosGeograficos.Add(ruta.Destino);
            var pDestino = await GetOrCreateParada(ruta.DestinoID);
            nuevasParadasRuta.Add(new RutaParada { RutaID = rutaId, ParadaID = pDestino.ParadaID, Orden = orden++ });

            _context.RutaParadas.AddRange(nuevasParadasRuta);

            double distanciaTotal = 0;
            for (int i = 0; i < puntosGeograficos.Count - 1; i++)
            {
                distanciaTotal += CalcularDistanciaHaversine(
                    (double)puntosGeograficos[i].Latitud, (double)puntosGeograficos[i].Longitud,
                    (double)puntosGeograficos[i + 1].Latitud, (double)puntosGeograficos[i + 1].Longitud
                );
            }

            ruta.DistanciaKM = (decimal)distanciaTotal;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Mapa actualizado", Distancia = Math.Round(distanciaTotal, 2) });
        }

        private async Task<Parada> GetOrCreateParada(int localidadId)
        {
            var parada = await _context.Paradas.FirstOrDefaultAsync(p => p.LocalidadID == localidadId);
            var loc = await _context.Localidades.FindAsync(localidadId);

            if (parada == null)
            {
                decimal latVal = loc.Latitud != 0 ? loc.Latitud : -34.6037m;
                decimal lonVal = loc.Longitud != 0 ? loc.Longitud : -58.3816m;

                parada = new Parada
                {
                    LocalidadID = localidadId,
                    NombreParada = "Terminal " + loc.NombreLocalidad,
                    Latitud = latVal,
                    Longitud = lonVal
                };
                _context.Paradas.Add(parada);
                await _context.SaveChangesAsync();
            }
            return parada;
        }

        private double CalcularDistanciaHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            if ((lat1 == 0 && lat2 == 0) || (lat1 == lat2 && lon1 == lon2)) return 50.0;

            var R = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle) => (Math.PI / 180) * angle;
    }
}
