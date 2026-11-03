
using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Availability.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Availability.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AvailabilityDbContext _context;

    public UsuarioRepository(AvailabilityDbContext context)
    {
        _context = context;
    }

    // Busca un usuario por su identidad autenticada.
    public async Task<Usuario?> ObtenerPorIdentidadAsync(string identidadExterna)
    {
        return await _context.Set<Usuario>()
            .FirstOrDefaultAsync(u => u.IdentidadExterna == identidadExterna);
    }

    // Registra un usuario nuevo y obtiene su ID generado por SQLite.
    public async Task<Usuario> CrearAsync(Usuario usuario)
    {
        _context.Set<Usuario>().Add(usuario);
        await _context.SaveChangesAsync();

        return usuario;
    }
}
