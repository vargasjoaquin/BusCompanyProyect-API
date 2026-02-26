using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_EmpresaBus_API.Data;
using Proyecto_EmpresaBus_API.Models;

namespace Proyecto_EmpresaBus_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmpresaController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public EmpresaController(ApiDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el listado de empresas registradas en el sistema.
        /// Este método suele ser utilizado por el frontend para el picker.
        /// </summary>
        /// <returns>Listado de empresas.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empresa>>> GetEmpresas()
        {
            return await _context.Empresas.ToListAsync();
        }

        /// <summary>
        /// Obtiene una empresa específica mediante su id.
        /// </summary>
        /// <param name="id">Id de la empresa.</param>
        /// <returns>Empresa encontrada.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Empresa>> GetEmpresa(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);

            if (empresa == null)
            {
                return NotFound();
            }

            return empresa;
        }

        /// <summary>
        /// Registra una nueva empresa en el sistema.
        /// Solo los usuarios con rol Administrador pueden realizar esta operación.
        /// </summary>
        /// <param name="empresa">Entidad empresa a crear.</param>
        /// <returns>Empresa creada.</returns>
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Empresa>> PostEmpresa(Empresa empresa)
        {
            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEmpresa", new { id = empresa.EmpresaID }, empresa);
        }

        /// <summary>
        /// Actualiza los datos de una empresa existente.
        /// Requiere permisos de Administrador.
        /// </summary>
        /// <param name="id">Id de la empresa.</param>
        /// <param name="empresa">Datos actualizados.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> PutEmpresa(int id, Empresa empresa)
        {
            if (id != empresa.EmpresaID)
            {
                return BadRequest();
            }

            _context.Entry(empresa).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmpresaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Elimina una empresa del sistema.
        /// La operación se bloquea si la empresa posee autobuses asociados.
        /// Requiere rol Administrador.
        /// </summary>
        /// <param name="id">Id de la empresa.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteEmpresa(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);

            if (empresa == null)
            {
                return NotFound();
            }

            // Validar si tiene autobuses asignados antes de borrar
            bool tieneBuses = await _context.Autobuses.AnyAsync(a => a.EmpresaID == id);
            
            if (tieneBuses)
            {
                return BadRequest("No se puede eliminar la empresa porque tiene autobuses asignados.");
            }

            _context.Empresas.Remove(empresa);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Verifica si una empresa existe en la base de datos.
        /// </summary>
        /// <param name="id">Id de la empresa.</param>
        /// <returns>True si existe; de lo contrario, false.</returns>
        private bool EmpresaExists(int id)
        {
            return _context.Empresas.Any(e => e.EmpresaID == id);
        }
    }
}