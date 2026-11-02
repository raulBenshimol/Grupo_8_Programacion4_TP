namespace Payment.Grpc.Models
{
    // Resultado del cobro que Business le devuelve a Services. Es independiente del contrato gRPC:
    // Services lo traduce a PaymentResponseDto, así Business no depende de la capa de presentación.
    public class PaymentResult
    {
        public bool Exitoso { get; set; }
        public string CodigoTransferencia { get; set; } = string.Empty; // Código de transferencia generado por la pasarela. Vacío si el pago fue rechazado.
        public string Mensaje { get; set; } = string.Empty; // Motivo legible del resultado (por ejemplo, el motivo del rechazo).
    }
}
