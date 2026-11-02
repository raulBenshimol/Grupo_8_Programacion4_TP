using Availability.Domain.Entities;
using Availability.Domain.Enum;

namespace Availability.Application.Interfaces
{
    public interface ISolicitudRepository
    {
        // Guarda una nueva solicitud de reserva.
        Task Guardar(SolicitudReserva solicitud);

        // Busca una solicitud por su identificador.
        Task<SolicitudReserva?> ObtenerPorId(Guid id);

        // Actualiza una solicitud existente.
        Task Actualizar(SolicitudReserva solicitud);

        // Actualiza la solicitud y el asiento en una misma operación.
        Task ActualizarConAsiento(
            SolicitudReserva solicitud,
            Asiento asiento);

        // Obtiene solicitudes que esperan una respuesta de Booking.
        Task<List<SolicitudReserva>> ObtenerPendientes();

        // Sincroniza los estados informados por Booking.
        // La implementación deberá proteger las retenciones
        // y evitar modificar asientos de otras solicitudes.
        Task<bool> SincronizarAsiento(
            Guid solicitudId,
            EstadoSolicitud nuevoEstado,
            EstadoAsiento? nuevoEstadoAsiento,
            MotivoRechazo? motivoRechazo = null);
    }
}
