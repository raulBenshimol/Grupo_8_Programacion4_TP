//Bruno Scucimarri
using System.Text.Json.Serialization;

namespace Booking.Booking.Infra.Messaging
{
    // Mensaje que publica Availability. Disallow rechaza campos que no estén acá, como pide
    // additionalProperties: false en SolicitudReserva.schema.json.
    // EventoId es int porque así lo publica Availability; el contrato todavía dice Guid.
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class SolicitudReservaMensaje
    {
        public required Guid SolicitudId { get; init; }
        public required int EventoId { get; init; }
        public required int AsientoId { get; init; }
        public required int UsuarioId { get; init; }
    }
}
