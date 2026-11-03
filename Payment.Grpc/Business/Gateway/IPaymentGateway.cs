namespace Payment.Grpc.Business.Gateway
{
    // Pasarela de pago: el punto donde Payment le pide a un procesador externo que cobre.
    // Business depende de esta interfaz y no de una implementación concreta, así que la
    // pasarela simulada del TP se podría reemplazar por una real sin tocar Business.
    public interface IPaymentGateway
    {
        Task<GatewayResult> CobrarAsync(Guid reservaId, long montoCentavos, bool simularRechazo);
    }
}
