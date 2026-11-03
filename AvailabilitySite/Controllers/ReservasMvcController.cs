using System.Security.Claims;
using System.Text.Json;
using Availability.Application.UseCases;
using Availability.Controllers.Controllers;
using Availability.Controllers.DTOs;
using AvailabilitySite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvailabilitySite.Controllers;

public class ReservasMvcController : Controller
{
    private readonly AvailabilityController _availabilityController;
    private readonly SeguimientoController _seguimientoController;
    private readonly ObtenerResultadoSolicitudUseCase _obtenerResultado;
    private readonly ConsultarDisponibilidadUseCase _consultarDisponibilidad;
    private readonly ILogger<ReservasMvcController> _logger;

    public ReservasMvcController(
        AvailabilityController availabilityController,
        SeguimientoController seguimientoController,
        ObtenerResultadoSolicitudUseCase obtenerResultado,
        ConsultarDisponibilidadUseCase consultarDisponibilidad,
        ILogger<ReservasMvcController> logger)
    {
        _availabilityController = availabilityController;
        _seguimientoController = seguimientoController;
        _obtenerResultado = obtenerResultado;
        _consultarDisponibilidad = consultarDisponibilidad;
        _logger = logger;
    }

    // GET: Mostrar la página completa de disponibilidad.
    [HttpGet]
    public async Task<IActionResult> Index(int eventoId = 1)
    {
        if (eventoId <= 0)
            return BadRequest("El evento debe ser válido.");

        var modelo = await ConstruirModeloAsientos(eventoId);

        return View(modelo);
    }

    // GET: Devolver únicamente el componente de asientos.
    // JavaScript utilizará este endpoint para actualizar la interfaz.
    [HttpGet]
    public async Task<IActionResult> AsientosParcial(int eventoId = 1)
    {
        if (eventoId <= 0)
            return BadRequest("El evento debe ser válido.");

        try
        {
            // Reutilizar el caso de uso existente.
            var modelo = await ConstruirModeloAsientos(eventoId);

            // Evitar respuestas antiguas guardadas en caché.
            Response.Headers.CacheControl = "no-store";

            // Renderizar solamente las tarjetas de asientos.
            return PartialView("_ListaAsientos", modelo);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AUDITORIA] Error al actualizar parcialmente los asientos. EventoId: {EventoId}",
                eventoId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "No se pudo actualizar la disponibilidad.");
        }
    }

    // Construye el modelo utilizado tanto por Index
    // como por la Partial View.
    private async Task<ReservasViewModel> ConstruirModeloAsientos(
        int eventoId)
    {
        var modelo = new ReservasViewModel
        {
            EventoId = eventoId
        };

        // Consultar todos los asientos a través del caso de uso.
        var asientos = await _consultarDisponibilidad
            .EjecutarTodos(eventoId);

        // Convertir entidades del dominio a DTO para MVC.
        modelo.Asientos = asientos.Select(a => new AsientoDto
        {
            Id = a.Id,
            Codigo = a.Codigo,
            Estado = a.Estado.ToString()
        }).ToList();

        if (modelo.Asientos.Count == 0)
        {
            modelo.Mensaje =
                "No se encontraron asientos para este evento.";
        }

        return modelo;
    }

    // POST: Solicitar una reserva con el usuario autenticado.
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarReserva(
        int eventoId,
        int asientoId)
    {
        // Obtener el ID numérico desde la sesión autenticada.
        var identificador = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(identificador, out int usuarioId) ||
            usuarioId <= 0)
        {
            return Forbid();
        }

        if (eventoId <= 0 || asientoId <= 0)
        {
            TempData["Error"] =
                "El evento o asiento seleccionado no es válido.";

            return RedirectToAction(
                nameof(Index),
                new { eventoId = 1 });
        }

        try
        {
            // Mantener la validación original: solo asientos libres.
            var asientos = await _availabilityController
                .ConsultarAsientos(eventoId);

            if (!asientos.Any(a => a.Id == asientoId))
            {
                TempData["Error"] =
                    "El asiento no está disponible para este evento.";

                return RedirectToAction(
                    nameof(Index),
                    new { eventoId });
            }

            // Preparar la solicitud con el usuario autenticado.
            var request = new SolicitudRequestDto
            {
                EventoId = eventoId,
                AsientoId = asientoId,
                UsuarioId = usuarioId
            };

            // Ejecutar el proceso de reserva existente.
            var resultado = await _availabilityController
                .SolicitarReserva(request);

            _logger.LogInformation(
                "[AUDITORIA] Solicitud recibida. SolicitudId: {SolicitudId}",
                resultado.SolicitudId);

            TempData["Mensaje"] =
                $"Solicitud recibida. Identificador: {resultado.SolicitudId}. " +
                "La reserva todavía no está confirmada.";

            // Conservar el identificador para consultar el estado.
            TempData["NuevaSolicitudId"] = resultado.SolicitudId.ToString();

            return RedirectToAction(
                nameof(Index),
                new { eventoId });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AUDITORIA] Error al solicitar reserva. EventoId: {EventoId}, AsientoId: {AsientoId}",
                eventoId,
                asientoId);

            TempData["Error"] =
                "No se pudo procesar la solicitud de reserva.";

            return RedirectToAction(
                nameof(Index),
                new { eventoId });
        }
    }

    // POST: Consultar el estado de una solicitud propia.
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConsultarEstado(
        string? solicitudId,
        int eventoId = 1)
    {
        // Validar el identificador de la solicitud.
        if (!Guid.TryParse(solicitudId, out Guid id) ||
            id == Guid.Empty)
        {
            TempData["Error"] =
                "Ingresá un identificador de solicitud válido.";

            return RedirectToAction(
                nameof(Index),
                new { eventoId });
        }

        // Obtener el identificador del usuario autenticado.
        var identificador = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(identificador, out int usuarioId) ||
            usuarioId <= 0)
        {
            return Forbid();
        }

        try
        {
            _logger.LogInformation(
                "[AUDITORIA] INICIO MVC - Consulta de estado. SolicitudId: {SolicitudId}",
                id);

            // Verificar que la solicitud pertenezca al usuario autenticado.
            var solicitud = await _obtenerResultado.Ejecutar(id);

            if (solicitud == null ||
                solicitud.UsuarioId != usuarioId)
            {
                _logger.LogWarning(
                    "[AUDITORIA] Consulta no autorizada o solicitud inexistente. SolicitudId: {SolicitudId}",
                    id);

                TempData["Error"] =
                    "No se encontró una solicitud asociada a tu cuenta.";

                return RedirectToAction(
                    nameof(Index),
                    new { eventoId });
            }

            // Consultar Booking mediante REST y sincronizar el estado.
            var resultado = await _seguimientoController
                .ObtenerEstado(id);

            // Guardar el resultado para mostrarlo en la tarjeta.
            TempData["ResultadoSeguimiento"] =
                JsonSerializer.Serialize(resultado);

            TempData["SolicitudConsultada"] = id.ToString();

            _logger.LogInformation(
                "[AUDITORIA] FIN MVC - Consulta de estado. SolicitudId: {SolicitudId}",
                id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AUDITORIA] ERROR MVC - Consulta de estado. SolicitudId: {SolicitudId}",
                id);

            TempData["Error"] =
                "No se pudo consultar el estado de la solicitud.";
        }

        return RedirectToAction(
            nameof(Index),
            new { eventoId });
    }
}
