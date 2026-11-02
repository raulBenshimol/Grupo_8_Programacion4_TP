namespace Payment.Grpc.Business.Gateway
{
    // Resultado que devuelve la pasarela.
    public class GatewayResult
    {
        public bool Exitoso { get; set; }

        public string CodigoTransferencia { get; set; } = string.Empty; // Código generado por la pasarela simulada. Vacío si el pago fue rechazado.

        public string Mensaje { get; set; } = string.Empty; // Motivo legible del resultado (por ejemplo, el motivo del rechazo).
    }
}
