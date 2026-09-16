using SkyHelp;
using Microsoft.EntityFrameworkCore;
using SkyHelp.Context;
using SkyHelp.Repositories.Interfaces;
using System.Linq.Expressions;
using SkyHelp.Models;
namespace SkyHelp.Repositories
{
    public class RolRepository : IRolRepository
    {
        private readonly SkyHelpContext _context;// Inyección de dependencia del contexto de la base de datos
        private readonly ILogger<RolRepository> _logger;
        public RolRepository(SkyHelpContext context, ILogger<RolRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<List<Roles>> ObtenerRoles()
        {
            return await _context.Roles.ToListAsync();
        }
        public async Task<Roles> ObtenerRolesPorID(Guid id)
        {
            return await _context.Roles.FirstOrDefaultAsync(x => x.IDRol == id);
        }
        public async Task<bool> AsignarRol(Roles roles)
        {
            try
            {
                await _context.Roles.AddAsync(roles);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el rol {NombreRol}", roles.NombreRol);
                return false;
            }
        }
        public async Task<bool> ActualizarRol(Roles rol)
        {
            try
            {
                var rolExistente = await _context.Roles.FirstOrDefaultAsync(x => x.IDRol == rol.IDRol);
                if (rolExistente == null)
                {
                    return false;
                }
                rolExistente.NombreRol = rol.NombreRol;
                rolExistente.Descripcion = rol.Descripcion;
                _context.Roles.Update(rolExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el rol {IDRol}", rol.IDRol);
                return false;
            }
        }

        public async Task<bool> EliminarRol(Guid id)
        {
            try
            {
                var rolExistente = await _context.Roles.FirstOrDefaultAsync(x => x.IDRol == id);
                if (rolExistente == null)
                {
                    return false;
                }
                _context.Roles.Remove(rolExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el rol {IDRol}", id);
                return false;
            }
        }
    }
}
