namespace Availability.Infrastructure.Messaging;

// el Contrato que Availability publica y Booking consume, serializado en JSON.
public class SolicitudReservaMensaje
{
    public Guid SolicitudId { get; set; }
    public int EventoId { get; set; }
    public int AsientoId { get; set; }
    public int UsuarioId { get; set; }

}