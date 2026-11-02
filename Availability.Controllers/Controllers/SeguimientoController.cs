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
        // 1. Buscar la solicitud en nuestra base SQLite.
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

        // 3. Leer el estado de Booking.
        // BookingHttpClient devuelve JsonElement para respuestas JSON.
        if (bookingResponse is JsonElement booking &&
            booking.ValueKind == JsonValueKind.Object &&
            booking.TryGetProperty("status", out var propiedadEstado) &&
            propiedadEstado.ValueKind == JsonValueKind.String)
        {
            var estadoBooking = propiedadEstado.GetString();

            if (string.Equals(
                estadoBooking,
                "Confirmada",
                StringComparison.OrdinalIgnoreCase))
            {
                // 4. Actualizar la solicitud y el asiento en SQLite.
                // Si falla, no ocultamos el problema.
                await _sincronizarEstado.Ejecutar(
                    solicitudId,
                    estadoBooking!);

                _logger.LogInformation(
                    "[AUDITORIA] Sincronización consultada. SolicitudId: {SolicitudId}",
                    solicitudId);
            }
        }

        // 5. Recuperar el estado local después de la sincronización.
        var solicitudActualizada = await _obtenerResultado
            .Ejecutar(solicitudId);

        // 6. Mantener la respuesta que utiliza nuestra vista Razor.
        return new
        {
            SolicitudId = solicitudId,
            EstadoLocal = solicitudActualizada?.Estado.ToString()
                ?? solicitud.Estado.ToString(),
            Booking = bookingResponse
        };
    }
}
