
using System.Security.Claims;
using Availability.Application.Interfaces;
using Availability.Controllers.Controllers;
using Availability.Controllers.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvailabilitySite.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservasApiController : ControllerBase
{
    private readonly AvailabilityController _availabilityLogicController;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<ReservasApiController> _logger;

    public ReservasApiController(
        AvailabilityController availabilityLogicController,
        IUsuarioRepository usuarioRepository,
        ILogger<ReservasApiController> logger)
    {
        _availabilityLogicController = availabilityLogicController;
        _usuarioRepository = usuarioRepository;
        _logger = logger;
    }

    // GET: api/ReservasApi/asientos?eventoId=1
    [HttpGet("asientos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConsultarAsientos([FromQuery] int eventoId)
    {
        _logger.LogInformation(
            "[AUDITORIA] INICIO API - Consulta de asientos. EventoId: {EventoId}",
            eventoId);

        if (eventoId <= 0)
        {
            _logger.LogWarning(
                "[AUDITORIA] FIN API - EventoId inválido: {EventoId}",
                eventoId);

            return BadRequest(new
            {
                Mensaje = "El identificador del evento debe ser mayor que cero."
            });
        }

        try
        {
            var asientos = await _availabilityLogicController
                .ConsultarAsientos(eventoId);

            _logger.LogInformation(
                "[AUDITORIA] FIN API - Consulta de asientos. EventoId: {EventoId}, Cantidad: {Cantidad}",
                eventoId, asientos.Count);

            return Ok(asientos);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AUDITORIA] ERROR API - Consulta de asientos. EventoId: {EventoId}",
                eventoId);

            return StatusCode(500, new
            {
                Mensaje = "Error interno al consultar los asientos."
            });
        }
    }

    // POST: api/ReservasApi/solicitar
    [Authorize]
    [HttpPost("solicitar")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SolicitarReserva(
        [FromBody] SolicitudRequestDto request)
    {
        // Obtener la identidad autenticada.
        var identificador = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        int usuarioId;

        if (int.TryParse(identificador, out int id) && id > 0)
        {
            // La sesión MVC contiene el ID numérico.
            usuarioId = id;
        }
        else
        {
            // Algunos tokens identifican al usuario por su correo.
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue("email");

            if (string.IsNullOrWhiteSpace(email))
                return Forbid();

            var usuario = await _usuarioRepository
                .ObtenerPorIdentidadAsync(
                    email.Trim().ToLowerInvariant());

            if (usuario == null)
                return Forbid();

            usuarioId = usuario.Id;
        }

        // Nunca confiar en el UsuarioId enviado por el cliente.
        request.UsuarioId = usuarioId;

        _logger.LogInformation(
            "[AUDITORIA] INICIO API - Solicitud de reserva. EventoId: {EventoId}, AsientoId: {AsientoId}",
            request.EventoId, request.AsientoId);

        if (request.EventoId <= 0 ||
            request.AsientoId <= 0 ||
            request.UsuarioId <= 0)
        {
            _logger.LogWarning(
                "[AUDITORIA] FIN API - Solicitud con identificadores inválidos.");

            return BadRequest(new
            {
                Mensaje = "Los identificadores del evento y asiento deben ser mayores que cero."
            });
        }

        try
        {
            // Comprobar que el asiento corresponda al evento
            // y aparezca entre los disponibles.
            var asientos = await _availabilityLogicController
                .ConsultarAsientos(request.EventoId);

            if (!asientos.Any(a => a.Id == request.AsientoId))
            {
                return BadRequest(new
                {
                    Mensaje = "El asiento no está disponible para este evento."
                });
            }

            var response = await _availabilityLogicController
                .SolicitarReserva(request);

            _logger.LogInformation(
                "[AUDITORIA] FIN API - Solicitud de reserva. SolicitudId: {SolicitudId}",
                response.SolicitudId);

            // Booking todavía debe procesar la reserva.
            return Accepted(value: response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AUDITORIA] ERROR API - Solicitud de reserva. EventoId: {EventoId}, AsientoId: {AsientoId}",
                request.EventoId, request.AsientoId);

            return StatusCode(500, new
            {
                Mensaje = "Error interno al procesar la solicitud."
            });
        }
    }
}
