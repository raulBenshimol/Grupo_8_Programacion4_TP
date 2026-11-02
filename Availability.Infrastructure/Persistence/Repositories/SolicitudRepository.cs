using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Availability.Application.Interfaces;
using Availability.Domain.Entities;
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

        // Guarda ambos cambios mediante una única transacción de EF Core.
        await _context.SaveChangesAsync();
    }
}
