namespace Availability.Application.Interfaces;

// Puerto hacia el AuthServer (servicio externo). La implementación está en Infrastructure.
public interface IAuthServerService
{
    // Token de servicio de Availability (OAuth2 Client Credentials), para llamar a Booking.
    Task<string> ObtenerTokenServicioAsync();

    // Cambia el id_token de Google por el token del sistema. Devuelve null si el AuthServer no valida la cuenta.
    Task<string?> ObtenerTokenUsuarioAsync(string idTokenGoogle);
}