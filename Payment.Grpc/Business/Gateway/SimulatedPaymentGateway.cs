namespace Payment.Grpc.Business.Gateway
{
    // Pasarela simulada del TP: reemplaza a un procesador de pagos real, que la consigna no pide integrar.
    // El resultado depende de simularRechazo, en lugar de tener el pago aprobado fijo.
    public class SimulatedPaymentGateway : IPaymentGateway
    {

        public Task<GatewayResult> CobrarAsync(Guid reservaId, long montoCentavos, bool simularRechazo)
        {
            if (simularRechazo)
            {
                return Task.FromResult(new GatewayResult
                {
                    Exitoso = false,
                    Mensaje = "Pago rechazado por la pasarela simulada."
                });
            }
           
            return Task.FromResult(new GatewayResult
            {
                Exitoso = true,
                CodigoTransferencia = Guid.NewGuid().ToString(), // Código de transferencia simulado.
                Mensaje = "Pago aprobado por la pasarela simulada."
            });
        }
    }
}
