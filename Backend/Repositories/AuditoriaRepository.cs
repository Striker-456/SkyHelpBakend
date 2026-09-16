using Microsoft.EntityFrameworkCore;
using SkyHelp;
using SkyHelp.Context;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;

namespace SkyHelp.Repositories
{
    public class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly SkyHelpContext _context;// Inyección de dependencia del contexto de la base de datos
        public AuditoriaRepository(SkyHelpContext context)
        {
            _context = context;
        }

        public async Task<List<Auditoria>> ObtenerAuditorias(string? usuario, string? accion, string? modulo, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Auditoria.Include(a => a.Usuario).AsQueryable();

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                var texto = usuario.Trim();
                query = query.Where(a =>
                    (a.Usuario != null && a.Usuario.NombreCompleto.Contains(texto)) ||
                    (a.Usuario != null && a.Usuario.Correo.Contains(texto)) ||
                    a.Descripcion.Contains(texto));
            }
            if (!string.IsNullOrWhiteSpace(accion))
                query = query.Where(a => a.TipoEvento == accion);
            if (!string.IsNullOrWhiteSpace(modulo))
                query = query.Where(a => a.TablaAfectada == modulo);
            if (desde.HasValue)
                query = query.Where(a => a.FechaEvento >= desde.Value);
            if (hasta.HasValue)
                query = query.Where(a => a.FechaEvento <= hasta.Value);

            return await query.OrderByDescending(a => a.FechaEvento).ToListAsync();
        }

        public async Task<Auditoria?> ObtenerAuditoriaPorID(Guid id)
        {
            return await _context.Auditoria.Include(a => a.Usuario).FirstOrDefaultAsync(x => x.IDLog == id);
        }

        public async Task<bool> CrearAuditoria(Auditoria auditoria)
        {
            try
            {
                await _context.Auditoria.AddAsync(auditoria);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
