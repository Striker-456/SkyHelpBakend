using SkyHelp.DTOs.Auditoria;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;
using SkyHelp.Services.Interfaces;

namespace SkyHelp.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly IAuditoriaRepository _auditoriaRepository;

        public AuditoriaService(IAuditoriaRepository auditoriaRepository)
        {
            _auditoriaRepository = auditoriaRepository;
        }

        public async Task RegistrarAsync(Guid idUsuario, string tipoEvento, string tablaAfectada, Guid idRegistro, string descripcion, string? ip)
        {
            var auditoria = new Auditoria
            {
                IDUsuario = idUsuario,
                TipoEvento = tipoEvento,
                TablaAfectada = tablaAfectada,
                IDRegistro = idRegistro,
                Descripcion = descripcion.Length > 300 ? descripcion[..300] : descripcion,
                FechaEvento = DateTime.UtcNow,
                DireccionIp = ip
            };

            // No debe interrumpir la operación que originó el evento si el registro de auditoría falla.
            await _auditoriaRepository.CrearAuditoria(auditoria);
        }

        public async Task<List<AuditoriaDto>> ObtenerAsync(AuditoriaFiltroDto filtro)
        {
            var lista = await _auditoriaRepository.ObtenerAuditorias(filtro.Usuario, filtro.Accion, filtro.Modulo, filtro.Desde, filtro.Hasta);
            return lista.Select(MapearDto).ToList();
        }

        public async Task<AuditoriaDto?> ObtenerPorIdAsync(Guid id)
        {
            var auditoria = await _auditoriaRepository.ObtenerAuditoriaPorID(id);
            return auditoria == null ? null : MapearDto(auditoria);
        }

        private static AuditoriaDto MapearDto(Auditoria a) => new()
        {
            Id = a.IDLog,
            FechaEvento = a.FechaEvento,
            IdUsuario = a.IDUsuario,
            Usuario = a.Usuario?.NombreCompleto ?? a.Usuario?.NombreUsuarios ?? a.Usuario?.Correo ?? "Desconocido",
            TipoEvento = a.TipoEvento,
            TablaAfectada = a.TablaAfectada,
            Descripcion = a.Descripcion,
            IdRegistro = a.IDRegistro,
            DireccionIp = a.DireccionIp
        };
    }
}
