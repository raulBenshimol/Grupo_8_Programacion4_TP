using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Availability.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infrastructure.Persistence.Repositories;

public class AsientoRepository : IAsientoRepository
{
    private readonly AvailabilityDbContext _context;

    public AsientoRepository(AvailabilityDbContext context)
    {
        _context = context;
    }

    // Obtiene solamente los asientos libres de un evento.
    public async Task<List<Asiento>> ObtenerDisponibles(int eventoId)
    {
        return await _context.Asientos
            .Where(a => a.EventoId == eventoId &&
                        a.Estado == EstadoAsiento.libre)
            .ToListAsync();
    }

    // Obtiene todos los asientos del evento, independientemente de su estado.
    public async Task<List<Asiento>> ObtenerTodosPorEvento(int eventoId)
    {
        return await _context.Asientos
            .Where(a => a.EventoId == eventoId)
            .OrderBy(a => a.Id)
            .ToListAsync();
    }

    // Busca un asiento por su identificador.
    public async Task<Asiento> ObtenerPorId(int asientoId)
    {
        return (await _context.Asientos
            .FirstOrDefaultAsync(a => a.Id == asientoId))!;
    }

    // Guarda los cambios realizados sobre un asiento en SQLite.
    public async Task Actualizar(Asiento asiento)
    {
        _context.Asientos.Update(asiento);
        await _context.SaveChangesAsync();
    }
}
