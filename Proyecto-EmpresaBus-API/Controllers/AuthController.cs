using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Proyecto_EmpresaBus_API.Data;
using Proyecto_EmpresaBus_API.Models;
using Proyecto_EmpresaBus_API.Request;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Proyecto_EmpresaBus_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApiDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                if (string.IsNullOrWhiteSpace(registerDto.NombreCompleto) ||
                    string.IsNullOrWhiteSpace(registerDto.Email) ||
                    string.IsNullOrWhiteSpace(registerDto.Password))
                {
                    return BadRequest("Nombre, Email y Password son obligatorios.");
                }

                var usuarioExistente = await _context.Usuarios
                                             .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

                if (usuarioExistente != null)
                {
                    return BadRequest($"El email '{registerDto.Email}' ya está registrado.");
                }

                // Intentar buscar LocalidadID si envían ciudad (Lógica opcional para compatibilidad)
                int? locId = null;
                if (!string.IsNullOrEmpty(registerDto.Ciudad))
                {
                    var loc = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad == registerDto.Ciudad);
                    if (loc != null) locId = loc.LocalidadID;
                }

                string passwordEncriptada = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                var nuevoUsuario = new Usuario
                {
                    NombreCompleto = registerDto.NombreCompleto.Trim(),
                    Email = registerDto.Email.Trim().ToLower(),
                    PasswordHash = passwordEncriptada, // RECOMENDACIÓN: Usar BCrypt para hashear
                    Rol = !string.IsNullOrEmpty(registerDto.Rol) ? registerDto.Rol : "Pasajero",
                    FechaCreacion = DateTime.UtcNow,
                    Direccion = registerDto.Direccion,
                    Telefono = registerDto.Telefono,
                    LocalidadID = locId // Asignamos ID si lo encontramos
                    // Sexo y Edad se pueden agregar al modelo Usuario si los necesitas
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Usuario registrado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error servidor: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto loginDto)
        {
            try
            {
                var emailNormalizado = loginDto.Email.Trim().ToLower();

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == emailNormalizado);

                if (usuario == null)
                {
                    return BadRequest("Email o contraseña incorrectos.");
                }

                bool passwordValida = BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.PasswordHash);

                if (!passwordValida)
                {
                    return BadRequest("Email o contraseña incorrectos.");
                }

                var claims = new[]
                {
                        new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioID.ToString()),
                        new Claim(ClaimTypes.Email, usuario.Email),
                        new Claim(ClaimTypes.Role, usuario.Rol)
                    };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds
                );

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new
                {
                    Message = "Login exitoso",
                    Token = jwt,
                    UsuarioId = usuario.UsuarioID,
                    Rol = usuario.Rol
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}