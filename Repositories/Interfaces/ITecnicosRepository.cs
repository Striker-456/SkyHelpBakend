
using SkyHelp.Models;

namespace SkyHelp.Repositories.Interfaces
{
    public interface ITecnicosRepository
    {
        Task<List<Tecnicos>> ObtenerTecnicos();
        Task<Tecnicos> ObtenerTecnicoPorId(Guid id);
        Task<Tecnicos?> ObtenerTecnicoPorIdUsuario(Guid idUsuario);
        Task<bool> CrearTecnico(Tecnicos tecnico);
        Task<bool> ActualizarTecnico(Tecnicos tecnico);
        Task<bool> EliminarTecnico(Guid id);
    }
}
