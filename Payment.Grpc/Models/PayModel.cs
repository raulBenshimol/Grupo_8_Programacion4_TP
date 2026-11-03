namespace Payment.Grpc.Models
{
    public class PayModel
    {
        public int Id { get; set; }
        public Guid SolicitudId { get; set; }
        public Guid ReservaId { get; set; }
        public bool Aprobado { get; set; }
        public long MontoCentavos { get; set; }
        public string CodigoTransferencia { get; set; } = string.Empty; // Código generado por la pasarela simulada. Vacío si el pago fue rechazado.
        public DateTime FechaTransaccion { get; set; }

    }
}