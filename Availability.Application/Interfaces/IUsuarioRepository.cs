using Availability.Domain.Entities;

namespace Availability.Application.Interfaces;

public interface IUsuarioRepository
{
    // Busca un usuario por su identidad autenticada.
    Task<Usuario?> ObtenerPorIdentidadAsync(string identidadExterna);

    // Registra un usuario nuevo en la base de datos.
    Task<Usuario> CrearAsync(Usuario usuario);
}