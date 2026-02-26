using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_EmpresaBus_API.Data;
using Proyecto_EmpresaBus_API.Dto;
using Proyecto_EmpresaBus_API.Models;
using Proyecto_EmpresaBus_API.Response;
using System.Security.Claims;

namespace Proyecto_EmpresaBus_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public UsuarioController(ApiDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el listado de usuarios.
        /// Si el usuario autenticado es Administrador, devuelve todos los usuarios.
        /// En caso contrario, devuelve únicamente sus propios datos.
        /// </summary>
        /// <returns>Lista de usuarios en formato DTO.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return Unauthorized("Usuario no autenticado");

            var rolUsuario = User.FindFirst(ClaimTypes.Role)?.Value;
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim)) return Unauthorized();
            var idUsuarioLogueado = int.Parse(idClaim);

            IQueryable<Usuario> query = _context.Usuarios
            .Include(u => u.Localidad)
            .ThenInclude(l => l.Provincia);

            if (rolUsuario != "Administrador")
            {
                query = query.Where(u => u.UsuarioID == idUsuarioLogueado);
            }

            var usuariosDto = await query.Select(u => new UsuarioResponseDto
            {
                UsuarioID = u.UsuarioID,
                NombreCompleto = u.NombreCompleto,
                Email = u.Email,
                DNI = u.DNI,
                Rol = u.Rol,
                FechaCreacion = u.FechaCreacion,
                Direccion = u.Direccion,
                Telefono = u.Telefono,
                Ciudad = u.Localidad != null ? u.Localidad.NombreLocalidad : "No especificada",
                FechaNacimiento = u.FechaNacimiento,
                Sexo = u.Sexo,
                Provincia = (u.Localidad != null && u.Localidad.Provincia != null)
                    ? u.Localidad.Provincia.NombreProvincia
                    : ""
            }).ToListAsync();

            return Ok(usuariosDto);
        }

        /// <summary>
        /// Obtiene los datos detallados de un usuario específico por su id.
        /// </summary>
        /// <param name="id">Id del usuario.</param>
        /// <returns>Datos del usuario en formato DTO.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioResponseDto>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Localidad)
                    .ThenInclude(l => l.Provincia)
                .FirstOrDefaultAsync(u => u.UsuarioID == id);

            if (usuario == null) return NotFound();

            var usuarioDto = new UsuarioResponseDto
            {
                UsuarioID = usuario.UsuarioID,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                DNI = usuario.DNI,
                Rol = usuario.Rol,
                FechaCreacion = usuario.FechaCreacion,
                Direccion = usuario.Direccion,
                Telefono = usuario.Telefono,
                FechaNacimiento = usuario.FechaNacimiento,
                Sexo = usuario.Sexo,
                Ciudad = usuario.Localidad?.NombreLocalidad,
                Provincia = usuario.Localidad?.Provincia?.NombreProvincia,
                LocalidadID = usuario.LocalidadID,
                ProvinciaID = usuario.Localidad?.ProvinciaID
            };

            return Ok(usuarioDto);
        }

        /// <summary>
        /// Actualiza la información de un usuario existente.
        /// Incluye validaciones de email y teléfono únicos,
        /// actualización de localidad y modificación opcional de contraseña.
        /// </summary>
        /// <param name="id">Id del usuario.</param>
        /// <param name="usuarioDto">Datos actualizados del usuario.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, UsuarioUpdateDto usuarioDto)
        {
            if (id != usuarioDto.UsuarioID) return BadRequest("ID no coincide.");

            var usuarioEnDb = await _context.Usuarios.FindAsync(id);
            if (usuarioEnDb == null) return NotFound();

            if (usuarioEnDb.Email.ToLower().Trim() != usuarioDto.Email.ToLower().Trim())
            {
                if (await _context.Usuarios.AnyAsync(u => u.Email == usuarioDto.Email && u.UsuarioID != id))
                    return BadRequest("El email ya está en uso.");
            }

            if (usuarioEnDb.Telefono != usuarioDto.Telefono)
            {
                bool existeTelefono = await _context.Usuarios
                        .AnyAsync(u => u.Telefono == usuarioDto.Telefono && u.UsuarioID != id);

                if (existeTelefono)
                {
                    return BadRequest("El número de teléfono ya está registrado por otro usuario.");
                }
            }

            usuarioEnDb.NombreCompleto = usuarioDto.NombreCompleto ?? usuarioEnDb.NombreCompleto;
            usuarioEnDb.Email = usuarioDto.Email ?? usuarioEnDb.Email;
            usuarioEnDb.DNI = usuarioDto.DNI ?? usuarioEnDb.DNI;
            usuarioEnDb.Telefono = usuarioDto.Telefono;
            usuarioEnDb.Direccion = usuarioDto.Direccion ?? usuarioEnDb.Direccion;
            usuarioEnDb.FechaNacimiento = usuarioDto.FechaNacimiento ?? usuarioEnDb.FechaNacimiento;
            usuarioEnDb.Sexo = usuarioDto.Sexo ?? usuarioEnDb.Sexo;
            usuarioEnDb.Rol = usuarioDto.Rol ?? usuarioEnDb.Rol;

            if (!string.IsNullOrEmpty(usuarioDto.Ciudad) && usuarioDto.ProvinciaID.HasValue)
            {
                var loc = await _context.Localidades.FirstOrDefaultAsync(l =>
                    l.NombreLocalidad.ToLower() == usuarioDto.Ciudad.ToLower() &&
                    l.ProvinciaID == usuarioDto.ProvinciaID);

                if (loc != null)
                {
                    usuarioEnDb.LocalidadID = loc.LocalidadID;
                }
                else // Si el usuario cambió la ciudad por una que no existe en DB, la creamos
                {
                    var nuevaLocalidad = new Localidad
                    {
                        NombreLocalidad = usuarioDto.Ciudad,
                        ProvinciaID = usuarioDto.ProvinciaID.Value,
                        Latitud = 0,
                        Longitud = 0
                    };
                    _context.Localidades.Add(nuevaLocalidad);
                    await _context.SaveChangesAsync(); // Obtenemos el nuevo ID
                    usuarioEnDb.LocalidadID = nuevaLocalidad.LocalidadID;
                }
            }

            if (!string.IsNullOrWhiteSpace(usuarioDto.Password))
            {
                usuarioEnDb.PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Password);
                _context.Entry(usuarioEnDb).Property(u => u.PasswordHash).IsModified = true;
            }

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error al guardar: " + ex.Message);
            }
        }

        /// <summary>
        /// Realiza una eliminación lógica del usuario (soft delete).
        /// Solo los Administradores pueden ejecutar esta acción.
        /// No permite eliminar cuentas con rol Administrador
        /// ni usuarios que posean viajes activos pendientes.
        /// </summary>
        /// <param name="id">Id del usuario.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound("Usuario no encontrado");

            if (usuario.Rol == "Administrador")
            {
                return BadRequest("No está permitido eliminar cuentas de Administrador.");
            }

            bool tieneBoletosActivos = await _context.Boletos
                .Include(b => b.Viaje)
                .AnyAsync(b => b.UsuarioID == id &&
                               !b.Viaje.IsDeleted);

            Console.WriteLine($"[DELETE USER] UsuarioID: {id}");
            Console.WriteLine($"[DELETE USER] ¿Tiene boletos activos?: {tieneBoletosActivos}");

            if (tieneBoletosActivos)
            {
                return BadRequest("No se puede eliminar: El usuario tiene viajes pendientes por realizar.");
            }

            usuario.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Usuario eliminado." });
        }

        // <summary>
        /// Restaura un usuario previamente eliminado (soft delete).
        /// Solo disponible para usuarios con rol Administrador.
        /// </summary>
        /// <param name="id">Id del usuario a restaurar.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Administrador")] // Solo el admin puede restaurar
        public async Task<IActionResult> RestoreUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.UsuarioID == id);

            if (usuario == null)
            {
                return NotFound("El usuario no existe en la base de datos.");
            }

            if (!usuario.IsDeleted)
            {
                return BadRequest("Este usuario ya está activo.");
            }

            usuario.IsDeleted = false;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = $"Usuario {usuario.NombreCompleto} restaurado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al restaurar: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el listado de usuarios eliminados lógicamente
        /// (papelera de reciclaje). Solo accesible para Administradores.
        /// </summary>
        /// <returns>Lista de usuarios eliminados.</returns>
        [HttpGet("deleted")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetDeletedUsuarios()
        {
            var eliminados = await _context.Usuarios
                .IgnoreQueryFilters()       
                .Where(u => u.IsDeleted)    
                .Include(u => u.Localidad)  
                .Select(u => new UsuarioResponseDto
                {
                    UsuarioID = u.UsuarioID,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email,
                    DNI = u.DNI,
                    Rol = u.Rol,
                    FechaCreacion = u.FechaCreacion,
                    Direccion = u.Direccion,
                    Telefono = u.Telefono,
                    Ciudad = u.Localidad != null ? u.Localidad.NombreLocalidad : null
                })
                .ToListAsync();

            return Ok(eliminados);
        }
    }
}
