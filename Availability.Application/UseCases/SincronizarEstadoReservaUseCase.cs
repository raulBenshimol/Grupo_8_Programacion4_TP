
using Availability.Application.Interfaces;
using Availability.Domain.Enum;
using Microsoft.Extensions.Logging;

namespace Availability.Application.UseCases;

public class SincronizarEstadoReservaUseCase
{
    private readonly ISolicitudRepository _solicitudes;
    private readonly IAsientoRepository _asientos;
    private readonly ILogger<SincronizarEstadoReservaUseCase> _logger;

    public SincronizarEstadoReservaUseCase(
        ISolicitudRepository solicitudes,
        IAsientoRepository asientos,
        ILogger<SincronizarEstadoReservaUseCase> logger)
    {
        _solicitudes = solicitudes;
        _asientos = asientos;
        _logger = logger;
    }

    public async Task Ejecutar(Guid solicitudId, string estadoBooking)
    {
        if (solicitudId == Guid.Empty ||
            string.IsNullOrWhiteSpace(estadoBooking))
        {
            return;
        }

        // Recuperar la solicitud original de Availability.
        var solicitud = await _solicitudes.ObtenerPorId(solicitudId);

        if (solicitud == null)
        {
            _logger.LogWarning(
                "[AUDITORIA] No existe la solicitud {SolicitudId}",
                solicitudId);
            return;
        }

        // En esta primera etapa procesamos únicamente confirmaciones.
        if (!estadoBooking.Equals(
                "Confirmada",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Evitar repetir una actualización ya aplicada.
        if (solicitud.Estado == EstadoSolicitud.Confirmada)
        {
            return;
        }

        // Buscar el asiento relacionado con la solicitud.
        var asiento = await _asientos.ObtenerPorId(solicitud.AsientoId);

        if (asiento == null || asiento.EventoId != solicitud.EventoId)
        {
            _logger.LogWarning(
                "[AUDITORIA] Asiento inválido para la solicitud {SolicitudId}",
                solicitudId);
            return;
        }

        // Actualizar ambos estados.
        solicitud.Estado = EstadoSolicitud.Confirmada;
        asiento.Estado = EstadoAsiento.ocupado;

        // Guardar ambos cambios en una única operación de SQLite.
        await _solicitudes.ActualizarConAsiento(solicitud, asiento);

        _logger.LogInformation(
            "[AUDITORIA] Reserva confirmada. SolicitudId: {SolicitudId}, AsientoId: {AsientoId}",
            solicitudId,
            asiento.Id);
    }
}
