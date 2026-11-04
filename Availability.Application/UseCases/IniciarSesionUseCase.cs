using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Availability.Application.UseCases;

// Resultado de un inicio de sesión válido
public record SesionIniciada(int UsuarioId, string Email, string AccessToken);

public class IniciarSesionUseCase
{
    private readonly IAuthServerService _authServer;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<IniciarSesionUseCase> _logger;

    public IniciarSesionUseCase(
        IAuthServerService authServer,
        IUsuarioRepository usuarioRepository,
        ILogger<IniciarSesionUseCase> logger)
    {
        _authServer = authServer;
        _usuarioRepository = usuarioRepository;
        _logger = logger;
    }

    // 1. El AuthServer valida el id_token de Google y emite el token del sistema.
    // 2. Se busca al usuario por su email o se lo registra si entra por primera vez.
    // Devuelve null si no se pudo iniciar sesión.
    public async Task<SesionIniciada?> Ejecutar(string? idTokenGoogle, string? email)
    {
        _logger.LogInformation("[AUDITORIA] INICIO - Inicio de sesión con Google.");

        if (string.IsNullOrWhiteSpace(idTokenGoogle) || string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("[AUDITORIA] FIN - Inicio de sesión rechazado: faltan datos de Google.");
            return null;
        }

        var accessToken = await _authServer.ObtenerTokenUsuarioAsync(idTokenGoogle);
        if (accessToken == null)
        {
            _logger.LogWarning("[AUDITORIA] FIN - Inicio de sesión rechazado por el AuthServer.");
            return null;
        }

        var identidadExterna = email.Trim().ToLowerInvariant();
        var usuario = await _usuarioRepository.ObtenerPorIdentidadAsync(identidadExterna)
                      ?? await _usuarioRepository.CrearAsync(new Usuario
                      {
                          IdentidadExterna = identidadExterna,
                          Email = email
                      });

        _logger.LogInformation("[AUDITORIA] FIN - Inicio de sesión correcto. UsuarioId: {UsuarioId}", usuario.Id);
        return new SesionIniciada(usuario.Id, email, accessToken);
    }
}