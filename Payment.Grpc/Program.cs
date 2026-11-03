using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Payment.Grpc.Business;
using Payment.Grpc.Business.Gateway;
using Payment.Grpc.Data;
using Payment.Grpc.Services;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();


// Capa de Datos: base propia de Payment en SQLite.Se elige SQLite por practicidad para el TP
// (un solo archivo, sin instalar un motor); en un sistema real se usaría SQL Server o PostgreSQL.
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PaymentDb")));

// Inyección de dependencias (N-Layer): cada capa pide interfaces y acá se decide qué implementación recibe.
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentGateway, SimulatedPaymentGateway>();
builder.Services.AddScoped<PaymentBusiness>();

// Logger de auditoría a archivo: Singleton para que todas las llamadas compartan el mismo
// "turno" de escritura (SemaphoreSlim) y no escriban el .txt al mismo tiempo.
builder.Services.AddSingleton<PaymentAuditLogger>();

// Validación del JWT emitido por el AuthServer: se verifica la firma con su clave PÚBLICA (RS256).
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

// Solo se puede llamar a Payment con un token válido de AuthServer
app.MapGrpcService<PaymentGrpcService>().RequireAuthorization();

// Configure the HTTP request pipeline.
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
