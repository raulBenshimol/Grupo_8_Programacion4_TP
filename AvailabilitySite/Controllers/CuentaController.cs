
using Availability.Domain.Entities;
using Availability.Application.Interfaces;
using System.Security.Claims;
using AvailabilitySite.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AvailabilitySite.Controllers;

// Autenticación con Google mediante AuthServer.
public class CuentaController : Controller
{
    private readonly TokenClient _tokenClient;
    private readonly IUsuarioRepository _usuarioRepository;

    public CuentaController(
        TokenClient tokenClient,
        IUsuarioRepository usuarioRepository)
    {
        _tokenClient = tokenClient;
        _usuarioRepository = usuarioRepository;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // 1. Redirigir al usuario a Google.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LoginGoogle(string? returnUrl)
    {
        var propiedades = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(DespuesDeGoogle), new { returnUrl })
        };

        return Challenge(propiedades, "Google");
    }

    // 2. Validar la autenticación con AuthServer.
    // 3. Buscar o registrar al usuario en SQLite.
    // 4. Guardar su identidad y su ID en la sesión.
    public async Task<IActionResult> DespuesDeGoogle(string? returnUrl)
    {
        var idTokenGoogle = await HttpContext.GetTokenAsync("id_token");

        var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("email");

        string? accessToken = null;

        try
        {
            if (idTokenGoogle != null)
            {
                accessToken = await _tokenClient
                    .ObtenerTokenUsuarioAsync(idTokenGoogle);
            }
        }
        catch (HttpRequestException)
        {
            // AuthServer no respondió.
        }

        if (accessToken == null || string.IsNullOrWhiteSpace(email))
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Error"] =
                "No se pudo iniciar sesión: el AuthServer no validó la cuenta de Google.";

            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        // Buscar al usuario por su identidad autenticada.
        var identidadExterna = email.Trim().ToLowerInvariant();

        var usuario = await _usuarioRepository
            .ObtenerPorIdentidadAsync(identidadExterna);

        // Registrar al usuario si ingresa por primera vez.
        if (usuario == null)
        {
            usuario = await _usuarioRepository.CrearAsync(new Usuario
            {
                IdentidadExterna = identidadExterna,
                Email = email
            });
        }

        // Incorporar el ID numérico a la identidad autenticada.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new("access_token", accessToken)
        };

        var identidad = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad));

        // Regresar a la página solicitada.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    // Cerrar la sesión.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }
}
