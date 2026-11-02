using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Availability.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infrastructure.Persistence.Repositories;

public class SolicitudRepository : ISolicitudRepository
{
    private readonly AvailabilityDbContext _context;

    public SolicitudRepository(AvailabilityDbContext context)
        => _context = context;

    // Guarda una nueva solicitud de reserva.
    public async Task Guardar(SolicitudReserva solicitud)
    {
        await _context.Solicitudes.AddAsync(solicitud);
        await _context.SaveChangesAsync();
    }

    // Busca una solicitud por su identificador.
    public async Task<SolicitudReserva?> ObtenerPorId(Guid id)
    {
        return await _context.Solicitudes
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    // Actualiza una solicitud existente.
    public async Task Actualizar(SolicitudReserva solicitud)
    {
        _context.Solicitudes.Update(solicitud);
        await _context.SaveChangesAsync();
    }

    // Actualiza la solicitud y su asiento en una misma operación.
    public async Task ActualizarConAsiento(
        SolicitudReserva solicitud,
        Asiento asiento)
    {
        _context.Solicitudes.Update(solicitud);
        _context.Asientos.Update(asiento);

        await _context.SaveChangesAsync();
    }

    // Obtiene las solicitudes que siguen esperando a Booking.
    public async Task<List<SolicitudReserva>> ObtenerPendientes()
    {
        return await _context.Solicitudes
            .AsNoTracking()
            .Where(s =>
                s.Estado == EstadoSolicitud.PendientePago ||
                s.Estado == EstadoSolicitud.Reservado)
            .ToListAsync();
    }

    // Refleja en SQLite un estado informado por Booking.
    // Protege los asientos asociados a otras solicitudes.
    public async Task<bool> SincronizarAsiento(
        Guid solicitudId,
        EstadoSolicitud nuevoEstado,
        EstadoAsiento? nuevoEstadoAsiento,
        MotivoRechazo? motivoRechazo = null)
    {
        // Todos los cambios deben realizarse en una transacción.
        await using var transaccion =
            await _context.Database.BeginTransactionAsync();

        var solicitud = await _context.Solicitudes
            .FirstOrDefaultAsync(s => s.Id == solicitudId);

        if (solicitud == null)
            return false;

        // Si el estado final ya se registró, no repetir cambios.
        if (solicitud.Estado == EstadoSolicitud.Confirmada ||
            solicitud.Estado == EstadoSolicitud.Rechazada ||
            solicitud.Estado == EstadoSolicitud.Vencida ||
            solicitud.Estado == EstadoSolicitud.Terminada)
        {
            return solicitud.Estado == nuevoEstado;
        }

        // Solo aceptamos los estados sincronizados desde Booking.
        if (nuevoEstado != EstadoSolicitud.Reservado &&
            nuevoEstado != EstadoSolicitud.Confirmada &&
            nuevoEstado != EstadoSolicitud.Rechazada &&
            nuevoEstado != EstadoSolicitud.Vencida)
        {
            return false;
        }

        var asiento = await _context.Asientos
            .FirstOrDefaultAsync(a =>
                a.Id == solicitud.AsientoId &&
                a.EventoId == solicitud.EventoId);

        if (asiento == null)
            return false;

        switch (nuevoEstado)
        {
            case EstadoSolicitud.Reservado:

                // Solo retener un asiento libre o uno que ya
                // pertenece a esta misma solicitud.
                if (asiento.Estado == EstadoAsiento.libre &&
                    asiento.SolicitudReservaId == null)
                {
                    asiento.Estado = EstadoAsiento.reservado;
                    asiento.SolicitudReservaId = solicitudId;
                }
                else if (asiento.Estado != EstadoAsiento.reservado ||
                         asiento.SolicitudReservaId != solicitudId)
                {
                    return false;
                }

                solicitud.Estado = EstadoSolicitud.Reservado;
                break;

            case EstadoSolicitud.Confirmada:

                // No ocupar un asiento retenido por otra solicitud.
                if (asiento.Estado == EstadoAsiento.ocupado)
                    return false;

                if (asiento.Estado == EstadoAsiento.reservado &&
                    asiento.SolicitudReservaId != solicitudId)
                {
                    return false;
                }

                // No aceptar una asociación incompatible.
                if (asiento.SolicitudReservaId != null &&
                    asiento.SolicitudReservaId != solicitudId)
                {
                    return false;
                }

                asiento.Estado = EstadoAsiento.ocupado;
                asiento.SolicitudReservaId = solicitudId;
                solicitud.Estado = EstadoSolicitud.Confirmada;
                break;

            case EstadoSolicitud.Rechazada:
            case EstadoSolicitud.Vencida:

                // Liberar solamente la retención de esta solicitud.
                if (asiento.Estado == EstadoAsiento.reservado &&
                    asiento.SolicitudReservaId == solicitudId)
                {
                    asiento.Estado = EstadoAsiento.libre;
                    asiento.SolicitudReservaId = null;
                }

                // No modificar asientos pertenecientes a otros.
                solicitud.Estado = nuevoEstado;

                if (nuevoEstado == EstadoSolicitud.Rechazada)
                {
                    solicitud.MotivoRechazo = motivoRechazo;
                }

                break;
        }

        // Guardar solicitud y asiento juntos.
        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return true;
    }
}
