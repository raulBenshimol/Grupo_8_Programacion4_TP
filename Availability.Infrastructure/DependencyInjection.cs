using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Availability.Application.Interfaces;
using Availability.Infrastructure.ExternalServices;
using Availability.Infrastructure.Messaging;
using Availability.Infrastructure.Persistence;
using Availability.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Availability.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Configurar la base de datos SQLite
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=availability.db";

        services.AddDbContext<AvailabilityDbContext>(
            options => options.UseSqlite(connectionString));

        // 2. Registrar los repositorios
        services.AddScoped<IAsientoRepository, AsientoRepository>();
        services.AddScoped<ISolicitudRepository, SolicitudRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        // 3. Registrar el Broker (Azure Service Bus)
        services.AddSingleton<IMessageBroker, AzureBroker>();

        // 4. AuthServer (servicio externo): tokens de servicio y de usuario
        // Singleton para reutilizar el token de servicio hasta que venza
        services.AddHttpClient();
        services.AddSingleton<IAuthServerService, AuthServerHttpClient>();
        services.AddTransient<TokenHandler>();


        return services;
    }
}
