
using Microsoft.EntityFrameworkCore;
using Availability.Domain.Entities;
using Availability.Domain.Enum;

namespace Availability.Infrastructure.Persistence;

public static class AsientosSeed
{
    public static async Task InicializarAsync(AvailabilityDbContext context)
    {
        // Crear ocho asientos para cada evento de prueba.
        for (int eventoId = 1; eventoId <= 3; eventoId++)
        {
            for (int numero = 1; numero <= 8; numero++)
            {
                string codigo = $"A{numero}";

                // Comprobar si el asiento ya existe en SQLite.
                bool existe = await context.Asientos.AnyAsync(a =>
                    a.EventoId == eventoId &&
                    a.Codigo == codigo);

                // Evitar duplicar asientos existentes.
                if (existe)
                    continue;

                // Los asientos nuevos comienzan libres.
                var asiento = new Asiento
                {
                    EventoId = eventoId,
                    Codigo = codigo,
                    Estado = EstadoAsiento.libre
                };

                context.Asientos.Add(asiento);
            }
        }

        // Guardar únicamente los asientos nuevos.
        await context.SaveChangesAsync();
    }
}
