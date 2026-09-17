using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyHelp.Authorization;
using SkyHelp.Models;
using SkyHelp.Repositories;
using SkyHelp.Repositories.Interfaces;
using System.Linq.Expressions;

namespace SkyHelp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RolController : ControllerBase
    {
        private readonly IRolRepository _RolRepository;
        public RolController(IRolRepository rolRepository)
        {
            _RolRepository = rolRepository;
        }

        [AllowAnonymous]
        [HttpGet("ObtenerRoles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> ObtenerRoles()
        {
            try
            {
                var roles = await _RolRepository.ObtenerRoles();
                if (roles == null || !roles.Any())
                {
                    return NotFound("No se encontraron roles.");
                }
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener Roles.");
            }
        }
        [HttpGet("ObtenerRolPorID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> ObtenerRolPorId(Guid ID)
        {
            try
            {
                var rol = await _RolRepository.ObtenerRolesPorID(ID);
                if (rol == null)
                {
                    return NotFound("Rol no encontrado.");
                }
                return Ok(rol);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el rol.");
            }
        }
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPost("AsignarRol")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AsignarRol([FromBody] Roles rol)
        {
            try
            {
                var resultado = await _RolRepository.AsignarRol(rol);
                if (!resultado)
                {
                    return BadRequest("No se pudo asignar el rol.");
                }
                return Ok("Rol asignado exitosamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al asignar el rol.");
            }
        }
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPut("ActualizarRol")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> ActualizarRol([FromBody] Roles rol)
            {
            try
            {
                var Resultado = await _RolRepository.ActualizarRol(rol);
                if (!Resultado)
                {
                    return BadRequest("No se puede Actualizar Rol");
                }
                return Ok("Rol Actualizado Correctamente");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al Actualizar Rol");
            }
        }


        [Authorize(Roles = RoleNames.Administrador)]
        [HttpDelete("EliminarRol")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EliminarRol(Guid Id)
        {
            try
            {
                var resultado = await _RolRepository.EliminarRol(Id);
                if (!resultado)
                {
                    return BadRequest("No se pudo Eliminar rol");
                }
                return Ok("Rol eliminiado correctamente");

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar el rol.");
            }
        }

    }
}
