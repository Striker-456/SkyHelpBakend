using SkyHelp.DTOs.Auditoria;

namespace SkyHelp.Services.Interfaces
{
    public interface IAuditoriaService
    {
        Task RegistrarAsync(Guid idUsuario, string tipoEvento, string tablaAfectada, Guid idRegistro, string descripcion, string? ip);
        Task<List<AuditoriaDto>> ObtenerAsync(AuditoriaFiltroDto filtro);
        Task<AuditoriaDto?> ObtenerPorIdAsync(Guid id);
    }
}
