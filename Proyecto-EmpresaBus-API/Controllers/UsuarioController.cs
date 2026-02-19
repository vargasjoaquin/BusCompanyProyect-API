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
                Edad = u.Edad,
                Sexo = u.Sexo,
                Provincia = (u.Localidad != null && u.Localidad.Provincia != null)
                    ? u.Localidad.Provincia.NombreProvincia
                    : ""
            }).ToListAsync();

            return Ok(usuariosDto);
        }

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
                Edad = usuario.Edad,
                Sexo = usuario.Sexo,
                LocalidadID = usuario.LocalidadID,
                ProvinciaID = usuario.Localidad?.ProvinciaID
            };

            return Ok(usuarioDto);
        }

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

            usuarioEnDb.NombreCompleto = usuarioDto.NombreCompleto;
            usuarioEnDb.Email = usuarioDto.Email;
            usuarioEnDb.DNI = usuarioDto.DNI;
            usuarioEnDb.Telefono = usuarioDto.Telefono;
            usuarioEnDb.Direccion = usuarioDto.Direccion;
            usuarioEnDb.Edad = usuarioDto.Edad;
            usuarioEnDb.Sexo = usuarioDto.Sexo;

            if (!string.IsNullOrEmpty(usuarioDto.Ciudad) && usuarioDto.ProvinciaID.HasValue)
            {
                var loc = await _context.Localidades.FirstOrDefaultAsync(l =>
                    l.NombreLocalidad.ToLower() == usuarioDto.Ciudad.ToLower() &&
                    l.ProvinciaID == usuarioDto.ProvinciaID);

                if (loc != null) usuarioEnDb.LocalidadID = loc.LocalidadID;
            }

            if (!string.IsNullOrWhiteSpace(usuarioDto.Password)) usuarioEnDb.PasswordHash = usuarioDto.Password;

            await _context.SaveChangesAsync();
            return NoContent();
        }

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

        // GET: api/Usuario/deleted
        // Endpoint para listar SOLO los usuarios eliminados (Papelera de Reciclaje)
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
