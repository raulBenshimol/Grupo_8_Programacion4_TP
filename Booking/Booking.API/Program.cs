using Azure.Messaging.ServiceBus;
using Booking.Booking.Business;
using Booking.Booking.Business.Interfaces;
using Booking.Booking.Data;
using Booking.Booking.Entities.Booking.Business.Interfaces;
using Booking.Booking.Entities.Interfaces;
using Booking.Booking.Infra.Auth;
using Booking.Booking.Infra.Messaging.Azure;
using Booking.Booking.Infra.Payment.Grpc;
using Microsoft.EntityFrameworkCore;
using SistemaReservas.Contracts.Payment.V1;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);
// El appsettings.json de Booking está dentro de la carpeta Booking.API, no en la raíz del proyecto.
builder.Configuration.AddJsonFile("Booking.API/appsettings.json", optional: false);

// Add services to the container.

builder.Services.AddControllers();
// Validación del JWT emitido por el AuthServer: se verifica la firma con su clave PÚBLICA (RS256).
// Booking puede comprobar tokens pero no fabricarlos (no tiene la clave privada).
var rsa = RSA.Create();
rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(builder.Configuration["Jwt:ClavePublica"]!), out _);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

//Bruno Scucimarri
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BookingDb") ?? "Data Source=booking.db"));
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

// ServiceBusClient es thread-safe y caro de crear: se comparte una sola instancia.
builder.Services.AddSingleton(_ => new ServiceBusClient(builder.Configuration.GetConnectionString("AzureServiceBus")));
builder.Services.AddSingleton<IValidator<SolicitudReservaMensaje>, SolicitudReservaValidator>();
builder.Services.AddHostedService<AzureServiceBusConsumer>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<AuthServerTokenProvider>();
// Booking pide su token al AuthServer (Client Credentials) y se agrega solo en cada llamada a Payment.
builder.Services.AddGrpcClient<PaymentService.PaymentServiceClient>(options =>
        options.Address = new Uri(builder.Configuration["Payment:Url"]!))
    .AddCallCredentials(async (context, metadata, serviceProvider) =>
    {
        string token = await serviceProvider.GetRequiredService<AuthServerTokenProvider>().ObtenerToken();
        metadata.Add("Authorization", $"Bearer {token}");
    });
builder.Services.AddScoped<IPaymentClient, GrpcPaymentClient>();

var app = builder.Build();

// Crea la base si no existe y aplica las migraciones pendientes antes de empezar a consumir mensajes.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<BookingDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // primero verifica quién sos (lee y valida el token)

app.UseAuthorization();

app.MapControllers();

app.Run();
