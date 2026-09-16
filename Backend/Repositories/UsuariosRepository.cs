using Microsoft.EntityFrameworkCore;
using SkyHelp;
using SkyHelp.Context;
using SkyHelp.Services.Security;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;
using System.Linq.Expressions;
namespace SkyHelp.Repositories

{
    public class UsuariosRepository : IUsuariosRepository
    {
        private readonly SkyHelpContext _context;
        private readonly ILogger<UsuariosRepository> _logger;
        public UsuariosRepository(SkyHelpContext context, ILogger<UsuariosRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<Usuarios> ObtenerUsuario(Guid id)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == id);
        }

        public async Task<Usuarios> ObtenerUsuarioPorCorreo(string Correo)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(x => x.Correo == Correo);
        }

        public async Task<List<Usuarios>> ObtenerUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }

        public async Task<bool> EliminarUsuario(Guid id)
        {
            try
            {
                var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == id);
                if (usuarioExistente == null)
                {
                    throw new Exception("Usuario para eliminar no existe");
                }

                // Eliminar registros relacionados primero
                // Eliminar domiciliarios asociados
                var domiciliarios = await _context.Domiciliarios.Where(d => d.IDUsuario == id).ToListAsync();
                if (domiciliarios.Count > 0)
                {
                    var idsDomiciliarios = domiciliarios.Select(d => d.IdDomiciliario).ToList();
                    // Desasignar (no borrar) los tickets de OTROS clientes que tengan a este
                    // domiciliario asignado: la FK Tickets.IdDomiciliario es ClientSetNull, así que
                    // EF sólo la pone en null si el ticket está cargado en el contexto.
                    var ticketsConDomiciliario = await _context.Tickets
                        .Where(t => t.IdDomiciliario != null && idsDomiciliarios.Contains(t.IdDomiciliario.Value))
                        .ToListAsync();
                    foreach (var ticket in ticketsConDomiciliario)
                        ticket.IdDomiciliario = null;
                }
                _context.Domiciliarios.RemoveRange(domiciliarios);

                // Eliminar técnicos asociados
                var tecnicos = await _context.Tecnicos.Where(t => t.IdUsuario == id).ToListAsync();
                if (tecnicos.Count > 0)
                {
                    var idsTecnicos = tecnicos.Select(t => t.IdTecnico).ToList();
                    // Mismo caso que arriba, pero para la asignación de técnico.
                    var ticketsConTecnico = await _context.Tickets
                        .Where(t => t.IdTecnico != null && idsTecnicos.Contains(t.IdTecnico.Value))
                        .ToListAsync();
                    foreach (var ticket in ticketsConTecnico)
                        ticket.IdTecnico = null;

                    // ProgresoTickets.IdTecnico también es ClientSetNull: conserva el historial de
                    // progreso registrado por este técnico, solo desvincula quién lo registró.
                    var progresosConTecnico = await _context.ProgresoTickets
                        .Where(p => p.IdTecnico != null && idsTecnicos.Contains(p.IdTecnico.Value))
                        .ToListAsync();
                    foreach (var progreso in progresosConTecnico)
                        progreso.IdTecnico = null;
                }
                _context.Tecnicos.RemoveRange(tecnicos);

                // Eliminar tickets asociados (como cliente)
                var ticketsCliente = await _context.Tickets.Where(t => t.IdUsuario == id).ToListAsync();
                _context.Tickets.RemoveRange(ticketsCliente);

                // Eliminar auditorías asociadas
                var auditorias = await _context.Auditoria.Where(a => a.IDUsuario == id).ToListAsync();
                _context.Auditoria.RemoveRange(auditorias);

                // Finalmente, eliminar el usuario
                _context.Usuarios.Remove(usuarioExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el usuario {IdUsuario}", id);
                throw;
            }
        }

        public async Task<bool> ActualizarUsuario( Usuarios usuario)
        {
            try
            {
                var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == usuario.IdUsuario);
                if (usuarioExistente == null)
                {
                    throw new Exception("Usuario para actualizar no existe");
                }

                usuarioExistente.NombreUsuarios = usuario.NombreUsuarios;
                usuarioExistente.IdRol = usuario.IdRol;
                usuarioExistente.NombreCompleto = usuario.NombreCompleto;
                usuarioExistente.Correo = usuario.Correo;
                usuarioExistente.EstadoCuenta = usuario.EstadoCuenta;
                usuarioExistente.Telefono = usuario.Telefono;

                if (!string.IsNullOrWhiteSpace(usuario.Contrasena))
                {
                    usuarioExistente.Contrasena = Seguridad.Hashear(usuario.Contrasena);
                }

                _context.Usuarios.Update(usuarioExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el usuario {IdUsuario}", usuario.IdUsuario);
                throw;
            }
        }

        public async Task<bool> CrearUsuario(Usuarios usuario)
        {
            try
            {
                usuario.Contrasena = Seguridad.Hashear(usuario.Contrasena);

                if (usuario.IdUsuario == Guid.Empty)
                {
                    usuario.IdUsuario = Guid.NewGuid();
                }

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el usuario {Correo}", usuario.Correo);
                throw; // propaga la excepción original con todo el inner exception
            }
        }
    }
}
