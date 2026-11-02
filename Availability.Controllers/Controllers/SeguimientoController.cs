using System;
using System.Text.Json;
using System.Threading.Tasks;
using Availability.Application.Interfaces;
using Availability.Application.UseCases;
using Microsoft.Extensions.Logging;

namespace Availability.Controllers.Controllers;

public class SeguimientoController
{
    private readonly ObtenerResultadoSolicitudUseCase _obtenerResultado;
    private readonly IBookingService _bookingService;
    private readonly SincronizarEstadoReservaUseCase _sincronizarEstado;
    private readonly ILogger<SeguimientoController> _logger;

    public SeguimientoController(
        ObtenerResultadoSolicitudUseCase obtenerResultado,
        IBookingService bookingService,
        SincronizarEstadoReservaUseCase sincronizarEstado,
        ILogger<SeguimientoController> logger)
    {
        _obtenerResultado = obtenerResultado;
        _bookingService = bookingService;
        _sincronizarEstado = sincronizarEstado;
        _logger = logger;
    }

    public async Task<object> ObtenerEstado(Guid solicitudId)
    {
        // 1. Buscar la solicitud original en Availability.
        var solicitud = await _obtenerResultado.Ejecutar(solicitudId);

        if (solicitud == null)
        {
            return new
            {
                Mensaje = "Solicitud no encontrada en Availability"
            };
        }

        object bookingResponse;

        // 2. Consultar Booking mediante el GET REST existente.
        try
        {
            bookingResponse = await _bookingService
                .ObtenerEstadoReserva(solicitudId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AUDITORIA] No se pudo consultar Booking. SolicitudId: {SolicitudId}",
                solicitudId);

            return new
            {
                SolicitudId = solicitudId,
                EstadoLocal = solicitud.Estado.ToString(),
                Booking = "No se pudo conectar con Booking."
            };
        }

        // 3. Interpretar el estado y el motivo de rechazo de Booking.
        if (bookingResponse is JsonElement booking &&
            booking.ValueKind == JsonValueKind.Object &&
            booking.TryGetProperty("status", out var propiedadEstado) &&
            propiedadEstado.ValueKind == JsonValueKind.String)
        {
            var estadoBooking = propiedadEstado.GetString();

            // El motivo puede ser null cuando no existe rechazo.
            string? motivoBooking = null;

            if (booking.TryGetProperty(
                    "rejectionMotive",
                    out var propiedadMotivo) &&
                propiedadMotivo.ValueKind == JsonValueKind.String)
            {
                motivoBooking = propiedadMotivo.GetString();
            }

            if (!string.IsNullOrWhiteSpace(estadoBooking))
            {
                // Availability refleja lo informado por Booking.
                await _sincronizarEstado.Ejecutar(
                    solicitudId,
                    estadoBooking,
                    motivoBooking);

                _logger.LogInformation(
                    "[AUDITORIA] Estado recibido de Booking: {Estado}. SolicitudId: {SolicitudId}",
                    estadoBooking,
                    solicitudId);
            }
        }

        // 4. Obtener el estado local después de la sincronización.
        var solicitudActualizada = await _obtenerResultado
            .Ejecutar(solicitudId);

        // 5. Conservar la respuesta utilizada por la vista Razor.
        return new
        {
            SolicitudId = solicitudId,
            EstadoLocal = solicitudActualizada?.Estado.ToString()
                ?? solicitud.Estado.ToString(),
            Booking = bookingResponse
        };
    }
}
