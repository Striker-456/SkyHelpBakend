using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkyHelp.Authorization;
using SkyHelp.Services.Security;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;
using SkyHelp.Services.Interfaces;
using System.Security.Claims;


namespace SkyHelp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuariosRepository _UsuariosRepository;
        private readonly IRolRepository _rolRepository;
        private readonly ITecnicosRepository _tecnicosRepository;
        private readonly IDomiciliariosRepository _domiciliariosRepository;
        private readonly IAuditoriaService _auditoriaService;
        public UsuariosController(IUsuariosRepository usuariosRepository, IRolRepository rolRepository, ITecnicosRepository tecnicosRepository, IDomiciliariosRepository domiciliariosRepository, IAuditoriaService auditoriaService)// Constructor de la clase con inyección de dependencia
        {
            _UsuariosRepository = usuariosRepository;// inyección de dependencia del repositorio de usuarios
            _rolRepository = rolRepository;
            _tecnicosRepository = tecnicosRepository;
            _domiciliariosRepository = domiciliariosRepository;
            _auditoriaService = auditoriaService;
        }

        private string? ObtenerIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private Guid ObtenerIdActor(Guid fallback)
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var id) ? id : fallback;
        }

        // Garantiza que todo usuario con rol Técnico tenga su fila en Tecnicos, sin importar si
        // se le asignó el rol al crearlo o al editarlo después — evita que un técnico "exista"
        // en Usuarios pero sea invisible en la lista de técnicos y en las asignaciones de tickets.
        private async Task AsegurarRegistroTecnico(Guid idRol, Guid idUsuario)
        {
            var rol = await _rolRepository.ObtenerRolesPorID(idRol);
            if (rol == null || RoleClaimMapper.ToJwtRole(rol.NombreRol) != RoleNames.Tecnico)
                return;

            var existente = await _tecnicosRepository.ObtenerTecnicoPorIdUsuario(idUsuario);
            if (existente == null)
                await _tecnicosRepository.CrearTecnico(new Tecnicos { IdUsuario = idUsuario, FechaRegistro = DateTime.UtcNow });
        }

        // Mismo propósito que AsegurarRegistroTecnico, para el rol Domiciliario: sin esto, un
        // usuario al que se le cambia el rol a Domiciliario (al crearlo o al editarlo) nunca
        // aparece en la tabla Domiciliarios ni en el panel de administración de domiciliarios.
        private async Task AsegurarRegistroDomiciliario(Guid idRol, Usuarios usuario)
        {
            var rol = await _rolRepository.ObtenerRolesPorID(idRol);
            if (rol == null || RoleClaimMapper.ToJwtRole(rol.NombreRol) != RoleNames.Domiciliario)
                return;

            var existente = await _domiciliariosRepository.ObtenerDomiciliarioPorIdUsuario(usuario.IdUsuario);
            if (existente == null)
            {
                await _domiciliariosRepository.CrearDomiciliario(new Domiciliarios
                {
                    IDUsuario = usuario.IdUsuario,
                    NombreCompleto = usuario.NombreCompleto,
                    Telefono = string.IsNullOrWhiteSpace(usuario.Telefono) ? "Sin registrar" : usuario.Telefono,
                    Email = usuario.Correo,
                    EstadoActividad = "Activo"
                });
            }
        }

        // Versión en lote de lo que antes era ObtenerNombrePorId: cualquier usuario autenticado (no solo admin)
        // puede resolver nombres de otros usuarios por Id — la necesitan, por ejemplo, un técnico
        // o domiciliario para mostrar el nombre del cliente en sus tickets/entregas, ya que
        // ObtenerUsuarios (el listado completo) es solo para administradores. Solo expone los
        // campos necesarios para mostrar un nombre, nunca el rol, estado de cuenta ni la
        // contraseña (hasheada) que sí trae ObtenerUsuarios.
        [Authorize]
        [HttpGet("ObtenerNombres")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerNombres()
        {
            try
            {
                var usuarios = await _UsuariosRepository.ObtenerUsuarios();
                var nombres = (usuarios ?? new List<Usuarios>())
                    .Select(u => new { u.IdUsuario, u.NombreCompleto, u.NombreUsuarios, u.Correo });
                return Ok(nombres);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los nombres de usuarios.");
            }
        }

        [Authorize(Roles = RoleNames.Administrador)]
        [HttpGet("ObtenerUsuarios")]// Definiendo que este método responde a solicitudes GET
        [ProducesResponseType(StatusCodes.Status200OK)]// Indicando que este método puede retornar un estado 200 OK
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]// Indicando que este método puede retornar un estado 500 Internal Server Error
        public async Task<IActionResult> ObtenerUsuarios()// Método para obtener todos los usuarios
        {
            try
            {
                var usuarios = await _UsuariosRepository.ObtenerUsuarios();// Llamando al método del repositorio para obtener los usuarios
                return Ok(usuarios ?? new List<Usuarios>());// Retornando una respuesta HTTP 200 con la lista de usuarios (vacía si no hay)
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener Usuarios.");// Retornando una respuesta HTTP 500 en caso de error
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("CrearUsuario")]// Definiendo que este método responde a solicitudes GET
        [ProducesResponseType(StatusCodes.Status200OK)]// Indicando que este método puede retornar un estado 200 OK
        [ProducesResponseType(StatusCodes.Status404NotFound)]// Indicando que este método puede retornar un estado 404 Not Found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]// Indicando que este método puede retornar un estado 500 Internal Server Error

        public async Task<IActionResult> CrearUsuario([FromBody] Usuarios usuario)
        {
            try
            {
                // El rol solicitado viene del cliente. Solo un Administrador ya autenticado puede
                // asignar el rol que quiera (p. ej. el modal de "Nuevo Usuario" del panel admin);
                // cualquier otra llamada (auto-registro público, o un usuario ya autenticado que
                // no sea admin) siempre crea una cuenta con el rol Cliente, sin importar qué
                // IdRol haya mandado — el tipo de usuario solo lo asigna el administrador.
                var llamanteEsAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole(RoleNames.Administrador);
                if (!llamanteEsAdmin)
                {
                    var roles = await _rolRepository.ObtenerRoles();
                    var rolCliente = roles.FirstOrDefault(r => RoleClaimMapper.ToJwtRole(r.NombreRol) == RoleNames.Usuario);
                    if (rolCliente == null)
                        return StatusCode(StatusCodes.Status500InternalServerError, "No se encontró el rol Cliente.");
                    usuario.IdRol = rolCliente.IDRol;
                }

                var Resultado = await _UsuariosRepository.CrearUsuario(usuario);

                if (!Resultado)
                {
                    return BadRequest("No se puede Crear Usuario");
                }
                await AsegurarRegistroTecnico(usuario.IdRol, usuario.IdUsuario);
                await AsegurarRegistroDomiciliario(usuario.IdRol, usuario);
                await _auditoriaService.RegistrarAsync(usuario.IdUsuario, "Crear", "Usuarios", usuario.IdUsuario,
                    $"Usuario {usuario.Correo} creado.", ObtenerIp());
                return Ok("Usuario Creado Correctamente");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al Insertar Persona");
            }
        }

        [Authorize]
        [HttpPut("CambiarContrasena")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaConVerificacionRequest request)
        {
            try
            {
                var correo = User.Identity?.Name;
                if (string.IsNullOrEmpty(correo))
                    return Unauthorized();

                var usuario = await _UsuariosRepository.ObtenerUsuarioPorCorreo(correo);
                if (usuario == null)
                    return NotFound("Usuario no encontrado.");

                // Verificar contraseña actual
                if (!Seguridad.Verificar(request.ContrasenaActual, usuario.Contrasena))
                    return BadRequest("Contraseña actual inválida.");

                usuario.Contrasena = request.NuevaContrasena;
                var resultado = await _UsuariosRepository.ActualizarUsuario(usuario);
                if (!resultado)
                    return BadRequest("No se pudo actualizar la contraseña.");

                return Ok("Contraseña actualizada correctamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al cambiar la contraseña.");
            }
        }

        [Authorize]
        [HttpPut("CambiarNombre")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CambiarNombre([FromBody] CambiarNombreRequest request)
        {
            try
            {
                var correo = User.Identity?.Name;
                if (string.IsNullOrEmpty(correo))
                    return Unauthorized();

                var usuario = await _UsuariosRepository.ObtenerUsuarioPorCorreo(correo);
                if (usuario == null)
                    return NotFound("Usuario no encontrado.");

                // Verificar contraseña
                if (!Seguridad.Verificar(request.Contrasena, usuario.Contrasena))
                    return BadRequest("Contraseña inválida.");

                // Crear objeto para actualizar sin cambiar contraseña
                var usuarioActualizado = new Usuarios
                {
                    IdUsuario = usuario.IdUsuario,
                    IdRol = usuario.IdRol,
                    NombreUsuarios = request.NuevoNombre.Split(' ')[0],
                    NombreCompleto = request.NuevoNombre,
                    Correo = usuario.Correo,
                    EstadoCuenta = usuario.EstadoCuenta,
                    Telefono = usuario.Telefono,
                    Contrasena = string.Empty // NO cambiar contraseña
                };
                
                var resultado = await _UsuariosRepository.ActualizarUsuario(usuarioActualizado);
                if (!resultado)
                    return BadRequest("No se pudo actualizar el nombre.");

                return Ok("Nombre actualizado correctamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al cambiar el nombre.");
            }
        }

        [Authorize]
        [HttpPut("CambiarCorreo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CambiarCorreo([FromBody] CambiarCorreoRequest request)
        {
            try
            {
                var correo = User.Identity?.Name;
                if (string.IsNullOrEmpty(correo))
                    return Unauthorized();

                var usuario = await _UsuariosRepository.ObtenerUsuarioPorCorreo(correo);
                if (usuario == null)
                    return NotFound("Usuario no encontrado.");

                // Verificar contraseña
                if (!Seguridad.Verificar(request.Contrasena, usuario.Contrasena))
                    return BadRequest("Contraseña inválida.");

                // Verificar que el nuevo correo no exista
                var usuarioExistente = await _UsuariosRepository.ObtenerUsuarioPorCorreo(request.NuevoCorreo);
                if (usuarioExistente != null)
                    return BadRequest("El correo ya está registrado.");

                // Crear objeto para actualizar sin cambiar contraseña
                var usuarioActualizado = new Usuarios
                {
                    IdUsuario = usuario.IdUsuario,
                    IdRol = usuario.IdRol,
                    NombreUsuarios = usuario.NombreUsuarios,
                    NombreCompleto = usuario.NombreCompleto,
                    Correo = request.NuevoCorreo,
                    EstadoCuenta = usuario.EstadoCuenta,
                    Telefono = usuario.Telefono,
                    Contrasena = string.Empty // NO cambiar contraseña
                };
                
                var resultado = await _UsuariosRepository.ActualizarUsuario(usuarioActualizado);
                if (!resultado)
                    return BadRequest("No se pudo actualizar el correo.");

                return Ok("Correo actualizado correctamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al cambiar el correo.");
            }
        }

        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPut("ActualizarUsuario")]// Definiendo que este método responde a solicitudes PUT
        [ProducesResponseType(StatusCodes.Status200OK)]// Indicando que este método puede retornar un estado 200 OK
        [ProducesResponseType(StatusCodes.Status404NotFound)]// Indicando que este método puede retornar un estado 404 Not Found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]// Indicando que este método puede retornar un estado 500 Internal Server Error

        public async Task<IActionResult> ActualizarUsuario([FromBody] ActualizarPerfilRequest usuario)
        {
            try
            {
                // Obtener el usuario existente
                var usuarioExistente = await _UsuariosRepository.ObtenerUsuario(usuario.IdUsuario);
                if (usuarioExistente == null)
                    return NotFound("Usuario no encontrado.");

                // Crear objeto Usuarios para actualizar
                var usuarioActualizado = new Usuarios
                {
                    IdUsuario = usuario.IdUsuario,
                    IdRol = usuario.IdRol ?? usuarioExistente.IdRol, // Usar nuevo rol si se proporciona, sino mantener el existente
                    NombreUsuarios = usuario.NombreUsuarios,
                    NombreCompleto = usuario.NombreCompleto,
                    Correo = usuario.Correo,
                    Contrasena = !string.IsNullOrWhiteSpace(usuario.Contrasena) ? usuario.Contrasena : string.Empty, // Usar nueva contraseña si se proporciona
                    EstadoCuenta = usuario.EstadoCuenta,
                    Telefono = usuario.Telefono
                };

                var Resultado = await _UsuariosRepository.ActualizarUsuario(usuarioActualizado);
                if (!Resultado)
                {
                    return BadRequest("No se puede Actualizar Usuario");
                }
                await AsegurarRegistroTecnico(usuarioActualizado.IdRol, usuarioActualizado.IdUsuario);
                await AsegurarRegistroDomiciliario(usuarioActualizado.IdRol, usuarioActualizado);
                await _auditoriaService.RegistrarAsync(ObtenerIdActor(usuario.IdUsuario), "Actualizar", "Usuarios", usuario.IdUsuario,
                    $"Usuario {usuarioActualizado.Correo} actualizado.", ObtenerIp());
                return Ok("Usuario Actualizado Correctamente");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al Actualizar Usuario");
            }
        }
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpDelete("EliminarUsuario")]// Definiendo que este método responde a solicitudes DELETE
        [ProducesResponseType(StatusCodes.Status200OK)]// Indicando que este método puede retornar un estado 200 OK
        [ProducesResponseType(StatusCodes.Status404NotFound)]// Indicando que este método puede retornar un estado 404 Not Found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]// Indicando que este método puede retornar un estado 500 Internal Server Error

        public async Task<IActionResult> EliminarUsuario(Guid ID)
        {
            try
            {
                var Resultado = await _UsuariosRepository.EliminarUsuario(ID);
                if (!Resultado)
                {
                    return BadRequest("No se Pudo Eliminar Al Usuario");
                }
                await _auditoriaService.RegistrarAsync(ObtenerIdActor(ID), "Eliminar", "Usuarios", ID,
                    $"Usuario {ID} eliminado.", ObtenerIp());
                return Ok("Usuario Eliminado Correctamente");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar  Usuario.");
            }
        }

        /// <summary>El rol Usuario solo puede actualizar su propio perfil; no puede cambiar su rol.</summary>
        [Authorize]
        [HttpPut("ActualizarMiPerfil")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActualizarMiPerfil([FromBody] ActualizarPerfilRequest perfilRequest)
        {
            try
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idClaim))
                    return Unauthorized();

                if (perfilRequest.IdUsuario != idClaim)
                    return Forbid();

                var existente = await _UsuariosRepository.ObtenerUsuario(perfilRequest.IdUsuario);
                if (existente == null)
                    return NotFound("Usuario no encontrado.");

                if (!string.Equals(existente.Correo, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                // Actualizar solo los campos permitidos
                existente.NombreUsuarios = perfilRequest.NombreUsuarios;
                existente.NombreCompleto = perfilRequest.NombreCompleto;
                existente.Telefono = perfilRequest.Telefono;
                existente.EstadoCuenta = perfilRequest.EstadoCuenta;

                // Crear un objeto Usuarios para pasar al repositorio
                var usuarioActualizado = new Usuarios
                {
                    IdUsuario = existente.IdUsuario,
                    IdRol = existente.IdRol,
                    NombreUsuarios = existente.NombreUsuarios,
                    NombreCompleto = existente.NombreCompleto,
                    Correo = existente.Correo,
                    Contrasena = string.Empty, // Pasar vacío para NO actualizar la contraseña
                    EstadoCuenta = existente.EstadoCuenta,
                    Telefono = existente.Telefono
                };

                var resultado = await _UsuariosRepository.ActualizarUsuario(usuarioActualizado);
                if (!resultado)
                    return BadRequest("No se puede actualizar el perfil.");
                return Ok("Perfil actualizado correctamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el perfil.");
            }
        }
    }
}

public class ActualizarPerfilRequest
{
    public Guid IdUsuario { get; set; }
    public string NombreUsuarios { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string EstadoCuenta { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Contrasena { get; set; } // Opcional - solo si se quiere cambiar
    public Guid? IdRol { get; set; } // Opcional - solo si se quiere cambiar el rol
}

public class CambiarContrasenaConVerificacionRequest
{
    public string ContrasenaActual { get; set; } = string.Empty;
    public string NuevaContrasena { get; set; } = string.Empty;
}

public class CambiarNombreRequest
{
    public string NuevoNombre { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

public class CambiarCorreoRequest
{
    public string NuevoCorreo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}
