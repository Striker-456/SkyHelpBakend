using SkyHelp.Models;

namespace SkyHelp.Repositories.Interfaces
{
    public interface IRolRepository
    {
        Task<List<Roles>> ObtenerRoles();
        Task<Roles> ObtenerRolesPorID(Guid id);

        Task<bool> AsignarRol(Roles roles);

        Task<bool> ActualizarRol( Roles roles);
        
        Task<bool> EliminarRol(Guid id);

    }
}
