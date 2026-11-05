using Availability.Application.UseCases;

namespace Availability.Controllers.Controllers;

// Controlador lógico (anillo 3-clean) del inicio de sesión: lo usa el CuentaController del sitio
public class AutenticacionController
{
    private readonly IniciarSesionUseCase _iniciarSesion;

    public AutenticacionController(IniciarSesionUseCase iniciarSesion)
    {
        _iniciarSesion = iniciarSesion;
    }

    public Task<SesionIniciada?> IniciarSesion(string? idTokenGoogle, string? email)
    {
        return _iniciarSesion.Ejecutar(idTokenGoogle, email);
    }
}