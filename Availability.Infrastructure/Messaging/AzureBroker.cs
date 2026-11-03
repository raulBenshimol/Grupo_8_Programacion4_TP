using System.Text.Json;
using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Availability.Infrastructure.Messaging;

// ADAPTADOR
public class AzureBroker : IMessageBroker, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureBroker> _logger;

    public AzureBroker(IConfiguration configuration, ILogger<AzureBroker> logger)
    {
        _logger = logger;

        // El connection string viene de user-secrets;
        // el nombre del tópico, de appsettings.json.
        var connectionString = configuration["ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException("Falta 'ServiceBus:ConnectionString'. Configurarla con dotnet user-secrets.");
        var topicName = configuration["ServiceBus:TopicName"]
            ?? throw new InvalidOperationException("Falta 'ServiceBus:TopicName' en appsettings.json.");

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
    }

    public async Task PublicarSolicitud(SolicitudReserva solicitud)
    {
        // Se traduce la entidad del dominio al contrato del mensaje: solo viaja lo que Booking necesita.
        var contenido = new SolicitudReservaMensaje
        {
            SolicitudId = solicitud.Id,
            EventoId = solicitud.EventoId,
            AsientoId = solicitud.AsientoId,
            UsuarioId = solicitud.UsuarioId
        };

        var mensaje = new ServiceBusMessage(JsonSerializer.Serialize(contenido))
        {
            ContentType = "application/json",
            Subject = "SolicitudReserva",
            MessageId = solicitud.Id.ToString(),
            CorrelationId = solicitud.Id.ToString()
        };

        // PUBLISH
        // A partir de acá el sistema es eventualmente consistente.
        // Availability ya respondió "recibida", pero la reserva todavía no existe en Booking.
        await _sender.SendMessageAsync(mensaje);

        _logger.LogInformation("[AUDITORIA] Availability publicó la solicitud {SolicitudId} en el tópico {Topico}",
            solicitud.Id, _sender.EntityPath);
    }

    // Al apagar la aplicación, el contenedor de dependencias llama a este método y cierra la conexión.
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}