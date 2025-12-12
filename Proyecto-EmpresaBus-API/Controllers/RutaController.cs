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
            int orden = 1;

            var puntosGeograficos = new List<Localidad>();

            puntosGeograficos.Add(ruta.Origen);

            var paradaOrigen = await GetOrCreateParada(ruta.OrigenID);
            nuevasParadasRuta.Add(new RutaParada { RutaID = rutaId, ParadaID = paradaOrigen.ParadaID, Orden = orden++ });

            if (dto.LocalidadesIds != null)
            {
                foreach (var locId in dto.LocalidadesIds)
                {
                    if (locId != ruta.OrigenID && locId != ruta.DestinoID)
                    {
                        var loc = await _context.Localidades.FindAsync(locId);
                        if (loc != null)
                        {
                            puntosGeograficos.Add(loc); 

                            var paradaIntermedia = await GetOrCreateParada(locId);
                            nuevasParadasRuta.Add(new RutaParada { RutaID = rutaId, ParadaID = paradaIntermedia.ParadaID, Orden = orden++ });
                        }
                    }
                }
            }

            puntosGeograficos.Add(ruta.Destino);

            var paradaDestino = await GetOrCreateParada(ruta.DestinoID);
            nuevasParadasRuta.Add(new RutaParada { RutaID = rutaId, ParadaID = paradaDestino.ParadaID, Orden = orden++ });

            _context.RutaParadas.AddRange(nuevasParadasRuta);


            double distanciaTotalKm = 0;

            for (int i = 0; i < puntosGeograficos.Count - 1; i++)
            {
                var puntoA = puntosGeograficos[i];
                var puntoB = puntosGeograficos[i + 1];

                distanciaTotalKm += CalcularDistanciaHaversine(
                    (double)puntoA.Latitud, (double)puntoA.Longitud,
                    (double)puntoB.Latitud, (double)puntoB.Longitud
                );
            }

            ruta.DistanciaKM = (decimal)distanciaTotalKm;
            _context.Entry(ruta).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Itinerario actualizado",
                TotalParadas = orden - 1,
                NuevaDistancia = Math.Round(distanciaTotalKm, 2)
            });
        }

        private async Task<Parada> GetOrCreateParada(int localidadId)
        {
            var parada = await _context.Paradas.FirstOrDefaultAsync(p => p.LocalidadID == localidadId);

            if (parada == null)
            {
                
                var loc = await _context.Localidades.FindAsync(localidadId);
                parada = new Parada
                {
                    LocalidadID = localidadId,
                    NombreParada = "Terminal " + loc.NombreLocalidad,
                    Latitud = 0, 
                    Longitud = 0
                };
                _context.Paradas.Add(parada);
                await _context.SaveChangesAsync();
            }
            return parada;
        }

        private double CalcularDistanciaHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            if (lat1 == 0 || lat2 == 0) return 100; // Valor por defecto si no hay coordenadas cargadas

            var R = 6371; // Radio de la tierra en KM
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;
            return d; // Retorna KM
        }

        private double ToRadians(double angle) => (Math.PI / 180) * angle;
    }
}
