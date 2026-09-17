using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyHelp.Authorization;
using SkyHelp.Models;
using SkyHelp.Repositories.Interfaces;
using SkyHelp.Services.Interfaces;
using System.Security.Claims;

namespace SkyHelp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketsRepository _ticketsRepository;
        private readonly ITecnicosRepository _tecnicosRepository;
        private readonly IDomiciliariosRepository _domiciliariosRepository;
        private readonly IProgresoTicketsRepository _progresoTicketsRepository;
        private readonly IAuditoriaService _auditoriaService;

        public TicketsController(
            ITicketsRepository ticketsRepository,
            ITecnicosRepository tecnicosRepository,
            IDomiciliariosRepository domiciliariosRepository,
            IProgresoTicketsRepository progresoTicketsRepository,
            IAuditoriaService auditoriaService)
        {
            _ticketsRepository = ticketsRepository;
            _tecnicosRepository = tecnicosRepository;
            _domiciliariosRepository = domiciliariosRepository;
            _progresoTicketsRepository = progresoTicketsRepository;
            _auditoriaService = auditoriaService;
        }

        private string? ObtenerIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private Guid ObtenerIdActor()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
        }

        // Admin: acceso total. Dueño del ticket, técnico asignado o domiciliario asignado: acceso al propio.
        private async Task<bool> PuedeGestionarTicket(Tickets ticket)
        {
            if (User.IsInRole(RoleNames.Administrador)) return true;

            var idActor = ObtenerIdActor();
            if (ticket.IdUsuario == idActor) return true;

            if (User.IsInRole(RoleNames.Tecnico))
            {
                var tecnico = await _tecnicosRepository.ObtenerTecnicoPorIdUsuario(idActor);
                if (tecnico != null && ticket.IdTecnico == tecnico.IdTecnico) return true;
            }

            if (User.IsInRole(RoleNames.Domiciliario))
            {
                var domiciliario = await _domiciliariosRepository.ObtenerDomiciliarioPorIdUsuario(idActor);
                if (domiciliario != null && ticket.IdDomiciliario == domiciliario.IdDomiciliario) return true;
            }

            return false;
        }

        // Resuelve el IdTecnico del actor autenticado cuando es un técnico (para dejar constancia de
        // quién hizo el registro de progreso); null si quien actúa es un Administrador.
        private async Task<Guid?> ObtenerIdTecnicoActorAsync()
        {
            if (!User.IsInRole(RoleNames.Tecnico)) return null;
            var tecnico = await _tecnicosRepository.ObtenerTecnicoPorIdUsuario(ObtenerIdActor());
            return tecnico?.IdTecnico;
        }

        [Authorize(Roles = RoleNames.Administrador)]
        [HttpGet("ObtenerTickets")]
        public async Task<IActionResult> ObtenerTickets()
        {
            try
            {
                var lista = await _ticketsRepository.ObtenerTickets();
                if (lista == null || !lista.Any())
                    return NotFound("No se encontraron tickets.");
                return Ok(lista);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los tickets.");
            }
        }

        [Authorize]
        [HttpGet("ObtenerTicketsAsignadosTecnico")]
        public async Task<IActionResult> ObtenerTicketsAsignadosTecnico()
        {
            try
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idUsuario))
                    return Unauthorized();

                var tecnico = await _tecnicosRepository.ObtenerTecnicoPorIdUsuario(idUsuario);
                if (tecnico == null)
                    return NotFound("No hay registro de técnico vinculado a este usuario.");

                var lista = await _ticketsRepository.ObtenerTicketsPorTecnico(tecnico.IdTecnico);
                return Ok(lista);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener los tickets asignados.");
            }
        }

        [Authorize]
        [HttpGet("ObtenerMisTickets")]
        public async Task<IActionResult> ObtenerMisTickets()
        {
            try
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var idUsuario))
                    return Unauthorized();

                var lista = await _ticketsRepository.ObtenerTicketsPorUsuario(idUsuario);
                return Ok(lista);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener sus tickets.");
            }
        }

        [Authorize]
        [HttpGet("ObtenerPorId")]
        public async Task<IActionResult> ObtenerTicketPorId(Guid Id)
        {
            try
            {
                var ticket = await _ticketsRepository.ObtenerTicketPorId(Id);
                if (ticket == null)
                    return NotFound("Ticket no encontrado.");
                if (!await PuedeGestionarTicket(ticket))
                    return Forbid();

                return Ok(ticket);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el ticket.");
            }
        }

        [Authorize]
        [HttpPost("CrearTicket")]
        public async Task<IActionResult> CrearTicket([FromBody] Tickets ticket)
        {
            try
            {
                var resultado = await _ticketsRepository.CrearTicket(ticket);
                if (!resultado)
                    return BadRequest("No se pudo crear el ticket.");
                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Crear", "Tickets", ticket.IdTicket,
                    $"Ticket #{ticket.NumeroTicket} creado.", ObtenerIp());
                return Ok("Ticket creado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al crear el ticket.");
            }
        }

        // Solo Administrador: la asignación real de domiciliario pasa por PedidosController.AsignarDomiciliario
        // (que exige diagnóstico previo). Se restringe este endpoint legado para que un técnico/domiciliario
        // no pueda saltarse esa regla de negocio llamándolo directamente.
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPut("ActualizarTicket")]
        public async Task<IActionResult> ActualizarTicket([FromBody] ActualizarDomiciliarioTicketRequest request)
        {
            try
            {
                if (request == null || request.IdTicket == Guid.Empty)
                    return BadRequest("El ticket no es válido o falta el ID.");

                var ticketExistente = await _ticketsRepository.ObtenerTicketPorId(request.IdTicket);
                if (ticketExistente == null)
                    return NotFound("Ticket no encontrado.");
                if (!await PuedeGestionarTicket(ticketExistente))
                    return Forbid();

                var resultado = await _ticketsRepository.ActualizarDomiciliarioTicket(request.IdTicket, request.IdDomiciliario);
                if (!resultado)
                    return StatusCode(StatusCodes.Status500InternalServerError, "No se pudo actualizar el domiciliario del ticket.");

                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Actualizar", "Tickets", request.IdTicket,
                    "Domiciliario del ticket actualizado.", ObtenerIp());
                return Ok("Domiciliario del ticket actualizado exitosamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarTicket: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPut("ActualizarEstadoTicket")]
        public async Task<IActionResult> ActualizarEstadoTicket([FromBody] ActualizarEstadoTicketRequest request)
        {
            try
            {
                if (request == null || request.IdTicket == Guid.Empty)
                    return BadRequest("El ticket no es válido o falta el ID.");

                if (request.IdEstado == Guid.Empty)
                    return BadRequest("El estado no es válido.");

                var ticketExistente = await _ticketsRepository.ObtenerTicketPorId(request.IdTicket);
                if (ticketExistente == null)
                    return NotFound("Ticket no encontrado.");
                if (!await PuedeGestionarTicket(ticketExistente))
                    return Forbid();

                // Usar SQL directo para actualizar solo el estado
                var resultado = await _ticketsRepository.ActualizarEstadoTicket(request.IdTicket, request.IdEstado);
                if (!resultado)
                    return StatusCode(StatusCodes.Status500InternalServerError, "No se pudo actualizar el estado del ticket.");

                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Actualizar", "Tickets", request.IdTicket,
                    "Estado del ticket actualizado.", ObtenerIp());
                return Ok("Estado del ticket actualizado exitosamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarEstadoTicket: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message}");
            }
        }

        // Acción dedicada y explícita para que el Administrador asigne (o reasigne) el técnico de un
        // ticket. Un solo técnico por ticket queda garantizado por naturaleza: IdTecnico es un campo
        // escalar, así que asignar uno nuevo siempre reemplaza al anterior.
        [Authorize(Roles = RoleNames.Administrador)]
        [HttpPut("AsignarTecnico")]
        public async Task<IActionResult> AsignarTecnico([FromBody] AsignarTecnicoRequest request)
        {
            try
            {
                if (request == null || request.IdTicket == Guid.Empty || request.IdTecnico == Guid.Empty)
                    return BadRequest("El ticket y el técnico son obligatorios.");

                var ticket = await _ticketsRepository.ObtenerTicketPorId(request.IdTicket);
                if (ticket == null)
                    return NotFound("Ticket no encontrado.");

                var tecnico = await _tecnicosRepository.ObtenerTecnicoPorId(request.IdTecnico);
                if (tecnico == null)
                    return NotFound("El técnico indicado no existe.");

                var resultado = await _ticketsRepository.AsignarTecnico(request.IdTicket, request.IdTecnico);
                if (!resultado)
                    return StatusCode(StatusCodes.Status500InternalServerError, "No se pudo asignar el técnico.");

                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Actualizar", "Tickets", request.IdTicket,
                    $"Técnico asignado al ticket #{ticket.NumeroTicket}.", ObtenerIp());
                return Ok("Técnico asignado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al asignar el técnico.");
            }
        }

        // El técnico asignado (o un Administrador) marca que comenzó el diagnóstico del equipo. Solo
        // registra la fecha de inicio (Tickets.FechaDiagnostico); el texto del diagnóstico llega
        // después con RegistrarDiagnostico, cuando el técnico "finaliza" el diagnóstico.
        [Authorize(Roles = $"{RoleNames.Administrador},{RoleNames.Tecnico}")]
        [HttpPut("IniciarDiagnostico")]
        public async Task<IActionResult> IniciarDiagnostico([FromBody] IniciarDiagnosticoRequest request)
        {
            try
            {
                if (request == null || request.IdTicket == Guid.Empty)
                    return BadRequest("El ticket es obligatorio.");

                var ticket = await _ticketsRepository.ObtenerTicketPorId(request.IdTicket);
                if (ticket == null)
                    return NotFound("Ticket no encontrado.");
                if (!await PuedeGestionarTicket(ticket))
                    return Forbid();

                var resultado = await _ticketsRepository.IniciarDiagnostico(request.IdTicket);
                if (!resultado)
                    return StatusCode(StatusCodes.Status500InternalServerError, "No se pudo iniciar el diagnóstico.");

                // Deja el primer registro de progreso del servicio (visible de inmediato para el
                // cliente en "Ver detalles") marcando el arranque del diagnóstico.
                await _progresoTicketsRepository.RegistrarProgreso(new ProgresoTickets
                {
                    IdTicket = request.IdTicket,
                    Porcentaje = 10,
                    Etapa = EtapasServicio.DiagnosticoIniciado,
                    Descripcion = "El técnico inició el diagnóstico del equipo.",
                    IdTecnico = await ObtenerIdTecnicoActorAsync()
                });

                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Actualizar", "Tickets", request.IdTicket,
                    $"Diagnóstico iniciado para el ticket #{ticket.NumeroTicket}.", ObtenerIp());
                return Ok("Diagnóstico iniciado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al iniciar el diagnóstico.");
            }
        }

        // El técnico asignado (o un Administrador) registra el diagnóstico del ticket. Este dato es el
        // que habilita, más adelante, que el Administrador pueda asignar un domiciliario (ver
        // PedidosController.AsignarDomiciliario).
        [Authorize(Roles = $"{RoleNames.Administrador},{RoleNames.Tecnico}")]
        [HttpPut("RegistrarDiagnostico")]
        public async Task<IActionResult> RegistrarDiagnostico([FromBody] RegistrarDiagnosticoRequest request)
        {
            try
            {
                if (request == null || request.IdTicket == Guid.Empty || string.IsNullOrWhiteSpace(request.Diagnostico))
                    return BadRequest("El ticket y el diagnóstico son obligatorios.");

                var ticket = await _ticketsRepository.ObtenerTicketPorId(request.IdTicket);
                if (ticket == null)
                    return NotFound("Ticket no encontrado.");
                if (!await PuedeGestionarTicket(ticket))
                    return Forbid();

                var resultado = await _ticketsRepository.RegistrarDiagnostico(
                    request.IdTicket, request.Diagnostico.Trim(),
                    request.FallaEncontrada?.Trim(), request.PruebasRealizadas?.Trim(),
                    request.Observaciones?.Trim(), request.Recomendaciones?.Trim());
                if (!resultado)
                    return StatusCode(StatusCodes.Status500InternalServerError, "No se pudo registrar el diagnóstico.");

                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Actualizar", "Tickets", request.IdTicket,
                    $"Diagnóstico registrado para el ticket #{ticket.NumeroTicket}.", ObtenerIp());
                return Ok("Diagnóstico registrado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al registrar el diagnóstico.");
            }
        }

        // Historial completo del progreso del servicio de un ticket (más reciente primero). El
        // propio cliente puede consultarlo desde "Ver detalles" (de ahí [Authorize] genérico en vez
        // de restringir a Administrador/Tecnico); PuedeGestionarTicket ya cubre esa regla de acceso.
        [Authorize]
        [HttpGet("ObtenerProgreso")]
        public async Task<IActionResult> ObtenerProgreso(Guid idTicket)
        {
            try
            {
                var ticket = await _ticketsRepository.ObtenerTicketPorId(idTicket);
                if (ticket == null)
                    return NotFound("Ticket no encontrado.");
                if (!await PuedeGestionarTicket(ticket))
                    return Forbid();

                var historial = await _progresoTicketsRepository.ObtenerHistorialPorTicket(idTicket);
                return Ok(historial);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener el progreso del ticket.");
            }
        }

        // El técnico asignado (o un Administrador) actualiza el avance del servicio: porcentaje,
        // etapa actual y una breve descripción de lo que se está haciendo. Cada llamada agrega una
        // fila nueva al historial (ver ObtenerProgreso) — es la acción detrás del control
        // "Actualizar progreso" de la pestaña Diagnóstico.
        [Authorize(Roles = $"{RoleNames.Administrador},{RoleNames.Tecnico}")]
        [HttpPut("ActualizarProgreso")]
        public async Task<IActionResult> ActualizarProgreso([FromBody] ActualizarProgresoRequest request)
        {
            try
            {
                if (request == null || request.IdTicket == Guid.Empty)
                    return BadRequest("El ticket es obligatorio.");
                if (request.Porcentaje < 0 || request.Porcentaje > 100)
                    return BadRequest("El porcentaje debe estar entre 0 y 100.");
                if (!EtapasServicio.EsValida(request.Etapa))
                    return BadRequest("La etapa seleccionada no es válida.");
                if (string.IsNullOrWhiteSpace(request.Descripcion))
                    return BadRequest("La descripción del progreso es obligatoria.");

                var ticket = await _ticketsRepository.ObtenerTicketPorId(request.IdTicket);
                if (ticket == null)
                    return NotFound("Ticket no encontrado.");
                if (!await PuedeGestionarTicket(ticket))
                    return Forbid();

                var registrado = await _progresoTicketsRepository.RegistrarProgreso(new ProgresoTickets
                {
                    IdTicket = request.IdTicket,
                    Porcentaje = request.Porcentaje,
                    Etapa = request.Etapa!.Trim(),
                    Descripcion = request.Descripcion.Trim(),
                    IdTecnico = await ObtenerIdTecnicoActorAsync()
                });
                if (!registrado)
                    return StatusCode(StatusCodes.Status500InternalServerError, "No se pudo actualizar el progreso.");

                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Actualizar", "Tickets", request.IdTicket,
                    $"Progreso del ticket #{ticket.NumeroTicket} actualizado a {request.Porcentaje}% ({request.Etapa}).", ObtenerIp());
                return Ok("Progreso actualizado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el progreso.");
            }
        }

        // Punto único de "Finalizar diagnóstico": persiste el texto del diagnóstico (y los campos
        // complementarios que el técnico haya diligenciado), y cierra la etapa de diagnóstico dejando
        // el progreso en 100% / "Diagnóstico finalizado". A partir de aquí el ticket queda listo para
        // que el Administrador decida si continúa hacia reparación (asignación de domiciliario) o se
        // resuelve directamente.
        [Authorize(Roles = $"{RoleNames.Administrador},{RoleNames.Tecnico}")]
        [HttpPut("FinalizarDiagnostico")]
        public async Task<IActionResult> FinalizarDiagnostico([FromBody] FinalizarDiagnosticoRequest request)
        {
            try
            {
                if (request == null || request.IdTicket == Guid.Empty)
                    return BadRequest("El ticket es obligatorio.");
                if (string.IsNullOrWhiteSpace(request.Diagnostico))
                    return BadRequest("Para finalizar el diagnóstico debes completar la descripción del diagnóstico.");
                if (string.IsNullOrWhiteSpace(request.DescripcionProgreso))
                    return BadRequest("Debes indicar una descripción para el progreso final del servicio.");

                var ticket = await _ticketsRepository.ObtenerTicketPorId(request.IdTicket);
                if (ticket == null)
                    return NotFound("Ticket no encontrado.");
                if (!await PuedeGestionarTicket(ticket))
                    return Forbid();

                var diagnosticoGuardado = await _ticketsRepository.RegistrarDiagnostico(
                    request.IdTicket, request.Diagnostico.Trim(),
                    request.FallaEncontrada?.Trim(), request.PruebasRealizadas?.Trim(),
                    request.Observaciones?.Trim(), request.Recomendaciones?.Trim());
                if (!diagnosticoGuardado)
                    return StatusCode(StatusCodes.Status500InternalServerError, "No se pudo guardar el diagnóstico.");

                var progresoRegistrado = await _progresoTicketsRepository.RegistrarProgreso(new ProgresoTickets
                {
                    IdTicket = request.IdTicket,
                    Porcentaje = 100,
                    Etapa = EtapasServicio.DiagnosticoFinalizado,
                    Descripcion = request.DescripcionProgreso.Trim(),
                    IdTecnico = await ObtenerIdTecnicoActorAsync()
                });
                if (!progresoRegistrado)
                    return StatusCode(StatusCodes.Status500InternalServerError, "No se pudo registrar el progreso final.");

                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Actualizar", "Tickets", request.IdTicket,
                    $"Diagnóstico finalizado para el ticket #{ticket.NumeroTicket} (100%).", ObtenerIp());
                return Ok("Diagnóstico finalizado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al finalizar el diagnóstico.");
            }
        }

        // Edición real de Categoría/Prioridad/Descripción — antes el modal "Editar Ticket" recolectaba
        // estos campos pero nunca llegaban a persistirse (el único endpoint que existía solo tocaba
        // IdDomiciliario).
        [Authorize]
        [HttpPut("ActualizarDetallesTicket")]
        public async Task<IActionResult> ActualizarDetallesTicket([FromBody] ActualizarDetallesTicketRequest request)
        {
            try
            {
                if (request == null || request.IdTicket == Guid.Empty ||
                    string.IsNullOrWhiteSpace(request.Categoria) || string.IsNullOrWhiteSpace(request.Prioridad) ||
                    string.IsNullOrWhiteSpace(request.Descripcion))
                    return BadRequest("Categoría, prioridad y descripción son obligatorias.");

                var ticket = await _ticketsRepository.ObtenerTicketPorId(request.IdTicket);
                if (ticket == null)
                    return NotFound("Ticket no encontrado.");
                if (!await PuedeGestionarTicket(ticket))
                    return Forbid();

                var resultado = await _ticketsRepository.ActualizarDetallesTicket(
                    request.IdTicket, request.Categoria.Trim(), request.Prioridad.Trim(), request.Descripcion.Trim());
                if (!resultado)
                    return StatusCode(StatusCodes.Status500InternalServerError, "No se pudieron actualizar los detalles del ticket.");

                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Actualizar", "Tickets", request.IdTicket,
                    $"Detalles del ticket #{ticket.NumeroTicket} actualizados.", ObtenerIp());
                return Ok("Ticket actualizado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al actualizar el ticket.");
            }
        }

        [Authorize(Roles = RoleNames.Administrador)]
        [HttpDelete("EliminarTicket")]
        public async Task<IActionResult> EliminarTicket(Guid Id)
        {
            try
            {
                var resultado = await _ticketsRepository.EliminarTicket(Id);
                if (!resultado)
                    return NotFound("Ticket no encontrado o no se pudo eliminar.");
                await _auditoriaService.RegistrarAsync(ObtenerIdActor(), "Eliminar", "Tickets", Id,
                    "Ticket eliminado.", ObtenerIp());
                return Ok("Ticket eliminado exitosamente.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al eliminar el ticket.");
            }
        }
    }

    public class ActualizarDomiciliarioTicketRequest
    {
        public Guid IdTicket { get; set; }
        public Guid? IdDomiciliario { get; set; }
    }

    public class ActualizarEstadoTicketRequest
    {
        public Guid IdTicket { get; set; }
        public Guid IdEstado { get; set; }
    }

    public class AsignarTecnicoRequest
    {
        public Guid IdTicket { get; set; }
        public Guid IdTecnico { get; set; }
    }

    public class IniciarDiagnosticoRequest
    {
        public Guid IdTicket { get; set; }
    }

    public class RegistrarDiagnosticoRequest
    {
        public Guid IdTicket { get; set; }
        public string Diagnostico { get; set; } = "";
        public string? FallaEncontrada { get; set; }
        public string? PruebasRealizadas { get; set; }
        public string? Observaciones { get; set; }
        public string? Recomendaciones { get; set; }
    }

    public class ActualizarProgresoRequest
    {
        public Guid IdTicket { get; set; }
        public int Porcentaje { get; set; }
        public string? Etapa { get; set; }
        public string? Descripcion { get; set; }
    }

    public class FinalizarDiagnosticoRequest
    {
        public Guid IdTicket { get; set; }
        public string Diagnostico { get; set; } = "";
        public string? FallaEncontrada { get; set; }
        public string? PruebasRealizadas { get; set; }
        public string? Observaciones { get; set; }
        public string? Recomendaciones { get; set; }
        public string DescripcionProgreso { get; set; } = "";
    }

    public class ActualizarDetallesTicketRequest
    {
        public Guid IdTicket { get; set; }
        public string Categoria { get; set; } = "";
        public string Prioridad { get; set; } = "";
        public string Descripcion { get; set; } = "";
    }
}
