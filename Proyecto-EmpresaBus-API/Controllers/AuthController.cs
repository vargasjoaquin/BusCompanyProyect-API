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

                if (registerDto == null) return BadRequest("Datos de registro no recibidos.");

                var emailLimpio = registerDto.Email.Trim().ToLower();

                if (await _context.Usuarios.AnyAsync(u => u.Email == emailLimpio))
                {
                    return BadRequest("El correo electrónico ingresado ya se encuentra registrado por otro usuario.");
                }

                if (await _context.Usuarios.AnyAsync(u => u.DNI == registerDto.DNI))
                {
                    return BadRequest("El DNI ingresado ya está asociado a una cuenta existente.");
                }

                if (await _context.Usuarios.AnyAsync(u => u.Telefono == registerDto.Telefono))
                {
                    return BadRequest("El número de teléfono ya está en uso por otro usuario.");
                }

                int? locId = null;
                if (!string.IsNullOrEmpty(registerDto.Ciudad))
                {
                    var loc = await _context.Localidades.FirstOrDefaultAsync(l => l.NombreLocalidad.ToLower() == registerDto.Ciudad.ToLower() && l.ProvinciaID == registerDto.ProvinciaID);
                    if (loc != null) locId = loc.LocalidadID;
                }

                string passwordEncriptada = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                var nuevoUsuario = new Usuario
                {
                    NombreCompleto = registerDto.NombreCompleto.Trim(),
                    Email = emailLimpio,
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

                return Ok(new { Message = "Usuario registrado exitosamente" });
            }
            catch (DbUpdateException)
            {
                return BadRequest("El Email, DNI o Teléfono ingresado ya está en uso.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Error del servidor: No se pudo completar el registro.");
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

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email.ToLower());
            if (usuario == null) return BadRequest("No existe una cuenta asociada a ese correo.");

            // Generamos un token aleatorio único
            string token = Guid.NewGuid().ToString();
            usuario.PasswordResetToken = token;
            usuario.ResetTokenExpires = DateTime.Now.AddHours(2); // Vence en 2 horas

            await _context.SaveChangesAsync();

            // Enviamos el correo (Cambiá el puerto por el que use tu MVC)
            string resetLink = $"https://localhost:44365/Auth/ResetPassword?token={token}";
            string mensaje = $"Hola {usuario.NombreCompleto},\n\nHemos recibido una solicitud para restablecer tu contraseña.\n" +
                             $"Ingresa al siguiente enlace para crear una nueva clave:\n\n{resetLink}\n\n" +
                             $"Si no solicitaste esto, ignora este mensaje.";

            await _emailService.SendEmailAsync(usuario.Email, "Restablecer Contraseña - Bux App", mensaje);

            return Ok(new { Message = "Se ha enviado un enlace de recuperación a tu correo." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == model.Token && u.ResetTokenExpires > DateTime.Now);

            if (usuario == null) return BadRequest("El enlace es inválido o ha expirado.");

            // Encriptamos la nueva contraseña
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Limpiamos el token
            usuario.PasswordResetToken = null;
            usuario.ResetTokenExpires = null;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Tu contraseña ha sido actualizada con éxito." });
        }

    }
}