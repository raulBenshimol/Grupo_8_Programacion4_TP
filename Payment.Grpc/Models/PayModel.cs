namespace Payment.Grpc.Models
{
    public class PayModel
    {
        public int Id { get; set; }
        public Guid SolicitudId { get; set; }
        public int ReservaId { get; set; }
        public bool Aprobado { get; set; }
        public int Monto { get; set; }
        public DateTime FechaTransaccion { get; set; }

    }
}