namespace Availability.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }

    // Identificador asociado al usuario autenticado.
    public string IdentidadExterna { get; set; } = string.Empty;

    // Correo electrónico del usuario.
    public string Email { get; set; } = string.Empty;
}
