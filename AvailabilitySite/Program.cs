using Serilog;
using Microsoft.OpenApi.Models;
using Availability.Application.Interfaces;
using Availability.Application.UseCases;
using Availability.Controllers.Presenters;
using Availability.Infrastructure;
using Availability.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AvailabilitySite.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using AvailabilitySite.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuracion de logs: consola y de archivo
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
    .MinimumLevel.Information()

    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)

    .WriteTo.Console()
    .WriteTo.File(
       "logs/availability-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7);
});

// 1. Infraestructura (Base de datos, repositorios, broker)
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Casos de Uso (Anillo 2)
builder.Services.AddScoped<SolicitarReservaInteractor>();
builder.Services.AddScoped<ConsultarDisponibilidadUseCase>();
builder.Services.AddScoped<ObtenerResultadoSolicitudUseCase>();

// Sincroniza el estado de las reservas con Booking.
builder.Services.AddScoped<SincronizarEstadoReservaUseCase>();

// Servicio que consulta automáticamente los estados en Booking.
builder.Services.AddHostedService<SincronizacionReservasWorker>();


// 3. Presenter (Anillo 3) - CAMBIO: Registramos una sola instancia para evitar el GUID en cero
builder.Services.AddScoped<SolicitudPresenter>();
builder.Services.AddScoped<ISolicitudOutputPort>(sp => sp.GetRequiredService<SolicitudPresenter>());


// Registramos los controladores lógicos del Anillo 3 para que puedan ser utilizados mediante inyección de dependencias.
// Controladores Lógicos del Anillo 3.1 
builder.Services.AddScoped<Availability.Controllers.Controllers.AvailabilityController>();
builder.Services.AddScoped<Availability.Controllers.Controllers.SeguimientoController>();

// Token de servicio: Availability se autentica ante Booking con el AuthServer (Client Credentials)
builder.Services.AddSingleton<TokenClient>();
builder.Services.AddTransient<TokenHandler>();

// CAMBIO: Cliente HTTP para llamar a Booking (el de tu compañero)
builder.Services.AddHttpClient<IBookingService, Availability.Infrastructure.ExternalServices.BookingHttpClient>(
    client =>
    {
        client.BaseAddress = new Uri("https://localhost:7109/");
    }).AddHttpMessageHandler<TokenHandler>(); // Para enviar el token al BookingService

// Login de usuarios con Google (proveedor) por OpenID Connect
// Después de Google, el sitio cambia el id_token por el token del AuthServer y recuerda al usuario con una cookie.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // igual que la duración del token del AuthServer
        // Las llamadas a /api responden 401 en vez de redirigir a la página de login.
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddOpenIdConnect("Google", options =>
    {
        options.Authority = "https://accounts.google.com";
        options.ClientId = builder.Configuration["Google:ClientId"];
        options.ClientSecret = builder.Configuration["Google:ClientSecret"]; // en User Secrets
        options.ResponseType = "code";            // Authorization Code (con PKCE)
        options.CallbackPath = "/signin-google";  // la URI de redirección registrada en Google
        options.Scope.Add("email");
        options.SaveTokens = true;                // guarda el id_token para enviarlo al AuthServer
    });
builder.Services.AddAuthorization();
// 4. MVC
builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Habilitar Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5. (Dejamos esto comentado por ahora para hacer las migraciones en el próximo paso)
/*
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
    db.Database.EnsureCreated(); 
}
*/


// Cargar asientos de prueba únicamente en desarrollo.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<AvailabilityDbContext>();

    await AsientosSeed.InicializarAsync(db);
}

app.UseStaticFiles();

app.UseRouting();
// Orden: primero quién sos, después si podés entrar
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}");
    pattern: "{controller=ReservasMvc}/{action=Index}/{id?}");

app.Run();


// Esta línea es para que las herramientas de EF Core puedan leer la configuración
public partial class Program { }