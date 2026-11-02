using Grpc.Core;
using Payment.Grpc.Business;
using SistemaReservas.Contracts.Payment.V1;

namespace Payment.Grpc.Services
{
    // Capa de Presentación (N-Layer): punto de entrada gRPC de Payment.
    // Hereda de la clase base que genera Grpc.Tools a partir de Contract/gRPC/payment.proto.
    // Solo valida y traduce entre el formato del contrato y Business; no toma decisiones de negocio.
    public class PaymentGrpcService : PaymentService.PaymentServiceBase
    {
        private readonly PaymentBusiness _business;

        public PaymentGrpcService(PaymentBusiness business)
        {
            _business = business;
        }

        public override async Task<PaymentResponseDto> ProcesarPago(PaymentRequestDto request, ServerCallContext context)
        {
            // Validación de entrada: si el pedido no cumple el contrato, se responde INVALID_ARGUMENT
            // sin llegar a Business (así lo documenta payment.proto).
            if (!Guid.TryParse(request.SolicitudId, out var solicitudId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "solicitud_id no es un UUID válido."));

            if (!Guid.TryParse(request.ReservaId, out var reservaId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "reserva_id no es un UUID válido."));

            if (request.MontoCentavos <= 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "monto_centavos debe ser mayor a cero."));

            var result = await _business.ProcesarPagoAsync(solicitudId, reservaId, request.MontoCentavos, request.SimularRechazo);

            // Traducción del resultado de Business al formato del contrato.
            return new PaymentResponseDto
            {
                Exitoso = result.Exitoso,
                Mensaje = result.Mensaje,
                CodigoTransferencia = result.CodigoTransferencia
            };
        }
    }
}