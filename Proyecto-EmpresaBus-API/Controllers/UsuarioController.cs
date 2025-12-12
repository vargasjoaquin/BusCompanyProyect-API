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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return Unauthorized("Usuario no autenticado");

            var rolUsuario = User.FindFirst(ClaimTypes.Role)?.Value;
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim)) return Unauthorized();
            var idUsuarioLogueado = int.Parse(idClaim);

            IQueryable<Usuario> query = _context.Usuarios.Include(u => u.Localidad);

            if (rolUsuario != "Administrador")
            {
                query = query.Where(u => u.UsuarioID == idUsuarioLogueado);
            }

            var usuariosDto = await query.Select(u => new UsuarioResponseDto
            {
                UsuarioID = u.UsuarioID,
                NombreCompleto = u.NombreCompleto,
                Email = u.Email,
                Rol = u.Rol,
                FechaCreacion = u.FechaCreacion,
                Direccion = u.Direccion,
                Telefono = u.Telefono,
                // Mapeo seguro de la localidad
                Ciudad = u.Localidad != null ? u.Localidad.NombreLocalidad : "No especificada"
            }).ToListAsync();

            return Ok(usuariosDto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioResponseDto>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Localidad)               // 1. Trae el objeto Localidad
                    .ThenInclude(l => l.Provincia)       // 2. Trae la Provincia dentro de esa Localidad
                .FirstOrDefaultAsync(u => u.UsuarioID == id);

            if (usuario == null) return NotFound();

            var usuarioDto = new UsuarioResponseDto
            {
                UsuarioID = usuario.UsuarioID,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = usuario.Rol,
                FechaCreacion = usuario.FechaCreacion,
                Direccion = usuario.Direccion,
                Telefono = usuario.Telefono,
                Edad = usuario.Edad,
                Sexo = usuario.Sexo,

                // 3. MAPEO MANUAL: Asignamos los nombres que vienen de las relaciones
                // Usamos el operador '?' para evitar errores si el usuario no tiene localidad asignada
                Ciudad = usuario.Localidad?.NombreLocalidad,
                Provincia = usuario.Localidad?.Provincia?.NombreProvincia
            };

            return Ok(usuarioDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, UsuarioUpdateDto usuarioDto)
        {
            if (id != usuarioDto.UsuarioID) return BadRequest("ID url no coincide con body.");

            var usuarioEnDb = await _context.Usuarios.FindAsync(id);
            if (usuarioEnDb == null) return NotFound("Usuario no encontrado.");

            // Validar Email único
            if (usuarioEnDb.Email.ToLower().Trim() != usuarioDto.Email.ToLower().Trim())
            {
                bool emailOcupado = await _context.Usuarios.AnyAsync(u => u.Email == usuarioDto.Email && u.UsuarioID != id);
                if (emailOcupado) return BadRequest("El email ya está en uso.");
            }

            usuarioEnDb.NombreCompleto = usuarioDto.NombreCompleto;
            usuarioEnDb.Email = usuarioDto.Email;
            usuarioEnDb.Telefono = usuarioDto.Telefono;
            usuarioEnDb.Direccion = usuarioDto.Direccion;
            usuarioEnDb.Edad = usuarioDto.Edad;
            usuarioEnDb.Sexo = usuarioDto.Sexo;

            // Intentar actualizar localidad si cambia el nombre de la ciudad
            if (!string.IsNullOrEmpty(usuarioDto.Ciudad))
            {
                var loc = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == usuarioDto.Ciudad);
                if (loc != null) usuarioEnDb.LocalidadID = loc.LocalidadID;
            }

            if (!string.IsNullOrEmpty(usuarioDto.Rol)) usuarioEnDb.Rol = usuarioDto.Rol;
            if (!string.IsNullOrWhiteSpace(usuarioDto.Password)) usuarioEnDb.PasswordHash = usuarioDto.Password;

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
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound("Usuario no encontrado");

            // Validar si tiene boletos para viajes que aún no ocurren
            bool tieneBoletosActivos = await _context.Boletos
                .Include(b => b.Viaje)
                .AnyAsync(b => b.UsuarioID == id &&
                               !b.Viaje.IsDeleted);

            Console.WriteLine($"[DELETE USER] UsuarioID: {id}");
            Console.WriteLine($"[DELETE USER] ¿Tiene boletos activos?: {tieneBoletosActivos}");

            if (tieneBoletosActivos)
            {
                // IMPORTANTE: Devolver un mensaje de texto plano
                return BadRequest("No se puede eliminar: El usuario tiene viajes pendientes por realizar.");
            }

            usuario.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Usuario eliminado." });
        }

        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Administrador")] // Solo el admin puede restaurar
        public async Task<IActionResult> RestoreUsuario(int id)
        {
            // Usamos IgnoreQueryFilters() para poder encontrar a los usuarios con IsDeleted = true
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

            // Lógica de restauración
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

        // GET: api/Usuario/deleted
        // Endpoint para listar SOLO los usuarios eliminados (Papelera de Reciclaje)
        [HttpGet("deleted")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetDeletedUsuarios()
        {
            var eliminados = await _context.Usuarios
                .IgnoreQueryFilters()       // Ignoramos el filtro global
                .Where(u => u.IsDeleted)    // Filtramos solo los borrados
                .Include(u => u.Localidad)  // Incluimos datos relacionados si es necesario
                .Select(u => new UsuarioResponseDto
                {
                    UsuarioID = u.UsuarioID,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email,
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
