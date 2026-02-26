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

        /// <summary>
        /// Obtiene todas las rutas registradas incluyendo origen, destino y paradas.
        /// </summary>
        /// <returns>Listado completo de rutas.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ruta>>> GetRutas()
        {
            return await _context.Rutas
                                 .Include(r => r.Origen)
                                 .Include(r => r.Destino)
                                 .Include(r => r.RutaParadas).ThenInclude(rp => rp.Parada)
                                 .ToListAsync();
        }

        /// <summary>
        /// Obtiene una ruta específica mediante su id.
        /// </summary>
        /// <param name="id">Id de la ruta.</param>
        /// <returns>Ruta encontrada.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Ruta>> GetRuta(int id)
        {
            var ruta = await _context.Rutas
                                     .Include(r => r.Origen)
                                     .Include(r => r.Destino)
                                     .FirstOrDefaultAsync(r => r.RutaID == id);

            if (ruta == null) 
                return NotFound();

            return ruta;
        }

        /// <summary>
        /// Crea una nueva ruta validando que origen y destino existan.
        /// La distancia es calculada automáticamente mediante fórmula Haversine.
        /// </summary>
        /// <param name="rutaDto">Datos necesarios para la creación de la ruta.</param>
        /// <returns>Ruta creada.</returns>
        [HttpPost]
        public async Task<ActionResult<Ruta>> PostRuta(RutaCreateDto rutaDto)
        {
            var origen = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Origen);
            var destino = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Destino);

            if (origen == null || destino == null)
            {
                return BadRequest("El Origen o Destino especificado no existe en la base de datos de Localidades.");
            }

            double latitud1 = (double)origen.Latitud;
            double longitud1 = (double)origen.Longitud;
            double latitud2 = (double)destino.Latitud;
            double longitud2 = (double)destino.Longitud;

            decimal distanciaCalculada = (decimal)CalcularDistanciaHaversine(latitud1, longitud1, latitud2, longitud2);

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

        // <summary>
        /// Actualiza los datos de una ruta existente.
        /// Permite modificar nombre, origen y destino.
        /// </summary>
        /// <param name="id">Id de la ruta.</param>
        /// <param name="rutaDto">Datos actualizados.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRuta(int id, RutaCreateDto rutaDto)
        {
            var ruta = await _context.Rutas.FindAsync(id);
            
            if (ruta == null) 
                return NotFound();

            var origen = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Origen);
            var destino = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == rutaDto.Destino);

            if (origen != null) 
                ruta.OrigenID = origen.LocalidadID;
            
            if (destino != null) 
                ruta.DestinoID = destino.LocalidadID;

            ruta.NombreRuta = rutaDto.NombreRuta;

            _context.Entry(ruta).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Rutas.Any(r => r.RutaID == id)) 
                    return NotFound();
                else 
                    throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Elimina una ruta del sistema.
        /// </summary>
        /// <param name="id">Id de la ruta.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRuta(int id)
        {
            var ruta = await _context.Rutas.FindAsync(id);

            if (ruta == null) 
                return NotFound();

            _context.Rutas.Remove(ruta);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Obtiene el itinerario de una ruta, ordenado geográficamente.
        /// Devuelve información de las paradas asociadas.
        /// </summary>
        /// <param name="id">Id de la ruta.</param>
        /// <returns>Listado de paradas.</returns>
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

            if (paradas == null || !paradas.Any()) 
                return Ok(new List<object>());

            return Ok(paradas);
        }

        /// <summary>
        /// Actualiza completamente el itinerario de una ruta.
        /// Recalcula automáticamente la distancia total en kilómetros.
        /// </summary>
        /// <param name="rutaId">Id de la ruta.</param>
        /// <param name="dto">Listado de localidades intermedias.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPost("{rutaId}/actualizar-itinerario")]
        public async Task<IActionResult> ActualizarItinerario(int rutaId, [FromBody] ItinerarioDto dto)
        {
            var ruta = await _context.Rutas
                .Include(r => r.Origen)
                .Include(r => r.Destino)
                .FirstOrDefaultAsync(r => r.RutaID == rutaId);

            if (ruta == null) 
                return NotFound("Ruta no encontrada");

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
                foreach (var localidadId in dto.LocalidadesIds)
                {
                    var localidades = await _context.Localidades.FindAsync(localidadId);
                    if (localidades != null && localidadId != ruta.OrigenID && localidadId != ruta.DestinoID)
                    {
                        puntosGeograficos.Add(localidades);
                        var pInt = await GetOrCreateParada(localidadId);
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

        /// <summary>
        /// Obtiene una parada asociada a una localidad o la crea si no existe.
        /// </summary>
        private async Task<Parada> GetOrCreateParada(int localidadId)
        {
            var parada = await _context.Paradas.FirstOrDefaultAsync(p => p.LocalidadID == localidadId);
            var localidades = await _context.Localidades.FindAsync(localidadId);

            if (parada == null)
            {
                decimal latitudValor = localidades.Latitud != 0 ? localidades.Latitud : -34.6037m;
                decimal longitudValor = localidades.Longitud != 0 ? localidades.Longitud : -58.3816m;

                parada = new Parada
                {
                    LocalidadID = localidadId,
                    NombreParada = "Terminal " + localidades.NombreLocalidad,
                    Latitud = latitudValor,
                    Longitud = longitudValor
                };
                _context.Paradas.Add(parada);
                await _context.SaveChangesAsync();
            }
            return parada;
        }

        /// <summary>
        /// Calcula la distancia entre dos puntos geográficos utilizando la fórmula Haversine.
        /// </summary>
        private double CalcularDistanciaHaversine(double latitud1, double longitud1, double latitud2, double longitud2)
        {
            if ((latitud1 == 0 && latitud2 == 0) || (latitud1 == latitud2 && longitud1 == longitud2)) 
                return 50.0;

            var radio = 6371;
            var distanciaLatitud = ToRadians(latitud2 - latitud1);
            var distanciaLongitud = ToRadians(longitud2 - longitud1);
            var average = Math.Sin(distanciaLatitud / 2) * Math.Sin(distanciaLatitud / 2) +
                    Math.Cos(ToRadians(latitud1)) * Math.Cos(ToRadians(latitud2)) *
                    Math.Sin(distanciaLongitud / 2) * Math.Sin(distanciaLongitud / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(average), Math.Sqrt(1 - average));
            return radio * c;
        }

        /// <summary>
        /// Convierte grados a radianes.
        /// </summary>
        private double ToRadians(double angle) => (Math.PI / 180) * angle;
    }
}
