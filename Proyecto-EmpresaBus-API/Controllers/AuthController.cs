using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Proyecto_EmpresaBus_API.Data;
using Proyecto_EmpresaBus_API.Interfaces;
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
        private readonly IEmailService _emailService;

        public AuthController(ApiDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
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

                var dniExistente = await _context.Usuarios
                                     .FirstOrDefaultAsync(u => u.DNI == registerDto.DNI);

                if (dniExistente != null)
                {
                    return BadRequest($"El DNI '{registerDto.DNI}' ya pertenece a otro usuario.");
                }

                if (!string.IsNullOrWhiteSpace(registerDto.Telefono))
                {
                    var telefonoExistente = await _context.Usuarios
                                                 .FirstOrDefaultAsync(u => u.Telefono == registerDto.Telefono);
                    if (telefonoExistente != null)
                    {
                        return BadRequest($"El teléfono '{registerDto.Telefono}' ya pertenece a otro usuario.");
                    }
                }

                int? locId = null;
                if (!string.IsNullOrEmpty(registerDto.Ciudad))
                {
                    var loc = await _context.Localidades
                                            .FirstOrDefaultAsync(l => l.NombreLocalidad == registerDto.Ciudad);

                    if (loc != null)
                    {
                        locId = loc.LocalidadID;
                    }
                    else
                    {
                        Console.WriteLine($"[AVISO] No se encontró la localidad: {registerDto.Ciudad}");
                    }
                }

                string passwordEncriptada = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                var nuevoUsuario = new Usuario
                {
                    NombreCompleto = registerDto.NombreCompleto.Trim(),
                    Email = registerDto.Email.Trim().ToLower(),
                    PasswordHash = passwordEncriptada,
                    DNI = registerDto.DNI,
                    Rol = !string.IsNullOrEmpty(registerDto.Rol) ? registerDto.Rol : "Pasajero",
                    FechaCreacion = DateTime.UtcNow,
                    Direccion = registerDto.Direccion,
                    Telefono = registerDto.Telefono,
                    LocalidadID = locId,
                    Edad = registerDto.Edad,
                    Sexo = registerDto.Sexo
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                try
                {
                    string asunto = "¡Bienvenido a Bux! - Registro Exitoso";

                    string cuerpo = $"Hola {nuevoUsuario.NombreCompleto},\n\n" +
                                    "Tu cuenta ha sido creada exitosamente en Bux App.\n\n" +
                                    "Tus datos de acceso son:\n" +
                                    $"Usuario: {nuevoUsuario.Email}\n\n" +
                                    "Gracias por elegir viajar con nosotros.\n" +
                                    "El equipo de Bux.";

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendEmailAsync(nuevoUsuario.Email, asunto, cuerpo);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error enviando email en segundo plano: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error preparando el email: {ex.Message}");
                }

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