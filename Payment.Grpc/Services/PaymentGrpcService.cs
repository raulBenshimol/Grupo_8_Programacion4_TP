using Grpc.Core;
using SistemaReservas.Contracts.Payment.V1;

namespace Payment.Grpc.Services
{
    // Capa de Presentación (N-Layer): punto de entrada gRPC de Payment.
    // Hereda de la clase base que genera Grpc.Tools a partir de Contract/gRPC/payment.proto.
    // Solo traduce entre el formato del contrato y Business; no toma decisiones de negocio.

    //NOTA PARA OWEN : si esto esta en Service, vamos a usar la Folder de Business? tal vez a la altura del program agregaria el controller.
    public class PaymentGrpcService : PaymentService.PaymentServiceBase
    {
        public override Task<PaymentResponseDto> ProcesarPago(PaymentRequestDto request, ServerCallContext context)
        {
            // TEMPORAL: respuesta fija que respeta simular_rechazo, para que Booking pueda probar
            // los caminos de pago aprobado y rechazado. Se reemplaza por la llamada a PaymentBusiness.
            var approved = !request.SimularRechazo;

            var response = new PaymentResponseDto
            {
                Exitoso = approved,
                Mensaje = approved
                    ? $"Pago de prueba aprobado para la reserva {request.ReservaId}"
                    : $"Pago de prueba rechazado para la reserva {request.ReservaId} (simular_rechazo = true)",
                CodigoTransferencia = approved ? "PRUEBA" : string.Empty
            };

            return Task.FromResult(response);
        }
    }
}