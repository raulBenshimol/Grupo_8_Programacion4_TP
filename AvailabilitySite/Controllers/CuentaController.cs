using System.Security.Claims;
using Availability.Controllers.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AvailabilitySite.Controllers;

// Autenticación con Google mediante AuthServer.
// El sitio solo maneja lo web (redirección a Google y cookie); la lógica está en el caso de uso IniciarSesion.
public class CuentaController : Controller
{
    private readonly AutenticacionController _autenticacionController;

    public CuentaController(AutenticacionController autenticacionController)
    {
        _autenticacionController = autenticacionController;
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

    // 2. Validar la cuenta con el AuthServer y buscar o registrar al usuario (caso de uso IniciarSesion).
    // 3. Guardar su identidad y su ID en la cookie.
    public async Task<IActionResult> DespuesDeGoogle(string? returnUrl)
    {
        var idTokenGoogle = await HttpContext.GetTokenAsync("id_token");

        var email = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("email");

        var sesion = await _autenticacionController.IniciarSesion(idTokenGoogle, email);

        if (sesion == null)
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Error"] =
                "No se pudo iniciar sesión: el AuthServer no validó la cuenta de Google.";

            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        // Incorporar el ID numérico a la identidad autenticada.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sesion.UsuarioId.ToString()),
            new(ClaimTypes.Name, sesion.Email),
            new(ClaimTypes.Email, sesion.Email),
            new("access_token", sesion.AccessToken)
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

        return RedirectToAction("Index", "ReservasMvc");
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