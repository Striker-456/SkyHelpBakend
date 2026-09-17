using SkyHelp.DTOs.Estadisticas;

namespace SkyHelp.Services.Interfaces
{
    public interface IEstadisticasService
    {
        Task<EstadisticasResumenDto> ObtenerResumenAsync(DateTime? desde, DateTime? hasta);
    }
}
