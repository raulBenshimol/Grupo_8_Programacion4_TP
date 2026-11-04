
using System.Text.Json;
using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Availability.Infrastructure.Messaging;

// Adaptador de mensajería con Azure Service Bus.
public class AzureBroker : IMessageBroker, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureBroker> _logger;

    public AzureBroker(
        IConfiguration configuration,
        ILogger<AzureBroker> logger)
    {
        _logger = logger;

        // Obtener la conexión desde User Secrets.
        var connectionString = configuration["ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Falta 'ServiceBus:ConnectionString'. Configurarla con dotnet user-secrets.");

        // Obtener el nombre del tópico desde appsettings.json.
        var topicName = configuration["ServiceBus:TopicName"]
            ?? throw new InvalidOperationException(
                "Falta 'ServiceBus:TopicName' en appsettings.json.");

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
    }

    // Publicar una solicitud de reserva.
    public async Task PublicarSolicitud(SolicitudReserva solicitud)
    {
        // Construir el mensaje respetando el contrato de Booking.
        // Solo enviamos los cuatro campos que Booking reconoce.
        var contenido = new SolicitudReservaMensaje
        {
            SolicitudId = solicitud.Id,
            EventoId = solicitud.EventoId,
            AsientoId = solicitud.AsientoId,
            UsuarioId = solicitud.UsuarioId
        };

        // Serializar el contrato y preparar el mensaje.
        var mensaje = new ServiceBusMessage(
            JsonSerializer.Serialize(contenido))
        {
            ContentType = "application/json",
            Subject = "SolicitudReserva",
            MessageId = solicitud.Id.ToString(),
            CorrelationId = solicitud.Id.ToString()
        };

        try
        {
            // Publicar el mensaje en Azure Service Bus.
            // A partir de aca el sistema se vuelve eventualmente consistente.
            await _sender.SendMessageAsync(mensaje);
        }
        catch (Exception ex)
        {
            // Si el broker no esta disponible, queda registrado que falló la publicacion (y no otra cosa),
            // de qué solicitud y hacia que topico. Se relanza para que el interactor decida qué responder.
            _logger.LogError(
                ex,
                "[AUDITORIA] ERROR - Availability no pudo publicar en el broker. SolicitudId: {SolicitudId}, tópico: {Topico}",
                solicitud.Id, _sender.EntityPath);
            throw;
        }

        _logger.LogInformation(
            "[AUDITORIA] Availability publicó en el broker. SolicitudId: {SolicitudId}, tópico: {Topico}, CorrelationId: {CorrelationId}",
            solicitud.Id, _sender.EntityPath, mensaje.CorrelationId);
    }
    // Liberar los recursos al detener la aplicación.
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
