using SkyHelp.Models;
namespace SkyHelp.Repositories.Interfaces
{
    public interface IAuditoriaRepository
    {
        Task<List<Auditoria>> ObtenerAuditorias(string? usuario, string? accion, string? modulo, DateTime? desde, DateTime? hasta);
        Task<Auditoria?> ObtenerAuditoriaPorID(Guid id);
        Task<bool> CrearAuditoria(Auditoria auditoria);
    }
}
