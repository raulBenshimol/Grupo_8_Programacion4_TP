using Availability.Application.Interfaces;
using Availability.Domain.Enum;
using Microsoft.Extensions.Logging;

namespace Availability.Application.UseCases;

public class SincronizarEstadoReservaUseCase
{
    private readonly ISolicitudRepository _solicitudes;
    private readonly ILogger<SincronizarEstadoReservaUseCase> _logger;

    public SincronizarEstadoReservaUseCase(
        ISolicitudRepository solicitudes,
        ILogger<SincronizarEstadoReservaUseCase> logger)
    {
        _solicitudes = solicitudes;
        _logger = logger;
    }

    public async Task Ejecutar(
        Guid solicitudId,
        string estadoBooking,
        string? motivoBooking = null)
    {
        if (solicitudId == Guid.Empty ||
            string.IsNullOrWhiteSpace(estadoBooking))
        {
            return;
        }

        // Interpretar el estado que Booking informa mediante GET REST.
        EstadoSolicitud nuevoEstado;

        switch (estadoBooking.Trim().ToLowerInvariant())
        {
            case "retenida":
                nuevoEstado = EstadoSolicitud.Reservado;
                break;

            case "confirmada":
                nuevoEstado = EstadoSolicitud.Confirmada;
                break;

            case "rechazada":
                nuevoEstado = EstadoSolicitud.Rechazada;
                break;

            case "vencida":
                nuevoEstado = EstadoSolicitud.Vencida;
                break;

            default:
                _logger.LogWarning(
                    "[AUDITORIA] Estado desconocido de Booking: {Estado}",
                    estadoBooking);
                return;
        }

        // Traducir el motivo informado por Booking.
        MotivoRechazo? motivo = motivoBooking?.Trim().ToLowerInvariant() switch
        {
            "asientoocupado" => MotivoRechazo.Ocupado,
            "pagofallido" => MotivoRechazo.PagoFallido,
            "pagonodisponible" => MotivoRechazo.PagoNoDisponible,
            _ => null
        };

        // El repositorio refleja el estado protegiendo las retenciones.
        bool actualizado = await _solicitudes.SincronizarAsiento(
            solicitudId,
            nuevoEstado,
            nuevoEstado switch
            {
                EstadoSolicitud.Reservado => EstadoAsiento.reservado,
                EstadoSolicitud.Confirmada => EstadoAsiento.ocupado,
                _ => null
            },
            motivo);

        if (actualizado)
        {
            _logger.LogInformation(
                "[AUDITORIA] Estado sincronizado. SolicitudId: {SolicitudId}, Estado: {Estado}",
                solicitudId,
                nuevoEstado);
        }
        else
        {
            _logger.LogWarning(
                "[AUDITORIA] No se pudo sincronizar la solicitud {SolicitudId}. Estado informado por Booking: {Estado}",
                solicitudId,
                nuevoEstado);
        }
    }
}
