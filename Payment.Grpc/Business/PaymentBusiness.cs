using Payment.Grpc.Business.Gateway;
using Payment.Grpc.Data;
using Payment.Grpc.Models;

namespace Payment.Grpc.Business
{
    // Capa de Negocio (N-Layer): reglas del cobro. Coordina la pasarela y el repositorio;
    // no conoce gRPC (eso es Services) ni EF Core (eso es Data).
    public class PaymentBusiness
    {
        private readonly IPaymentRepository _repository;
        private readonly IPaymentGateway _gateway;
        private readonly ILogger<PaymentBusiness> _logger;
        private readonly PaymentAuditLogger _auditLogger;

        public PaymentBusiness(IPaymentRepository repository, IPaymentGateway gateway, ILogger<PaymentBusiness> logger, PaymentAuditLogger auditLogger)
        {
            _repository = repository;
            _gateway = gateway;
            _logger = logger;
            _auditLogger = auditLogger;
        }

        public async Task<PaymentResult> ProcesarPagoAsync(Guid solicitudId, Guid reservaId, long montoCentavos, bool simularRechazo)
        {
            // Auditoría (punto f): se registra el inicio y el fin con la solicitud y la reserva,
            // para seguir la cadena de sucesos entre servicios. Se escribe en la consola (ILogger)
            // y en Data/payment-audit-log.txt (PaymentAuditLogger), para que quede guardado.
            _logger.LogInformation("[AUDITORIA] INICIO Payment - Solicitud {SolicitudId}, reserva {ReservaId}", solicitudId, reservaId);
            await _auditLogger.LogAsync($"[AUDITORIA] INICIO Payment - Solicitud {solicitudId}, reserva {reservaId}");

            // Idempotencia: si la reserva ya se cobró (por ejemplo, un reintento de Booking),
            // no se vuelve a cobrar y se devuelve el resultado original.
            var pagoExistente = await _repository.ObtenerPorReservaAsync(reservaId);
            if (pagoExistente != null)
            {
                _logger.LogInformation("[AUDITORIA] FIN Payment - Solicitud {SolicitudId}, reserva {ReservaId}: cobro repetido, se devuelve el resultado original", solicitudId, reservaId);
                await _auditLogger.LogAsync($"[AUDITORIA] FIN Payment - Solicitud {solicitudId}, reserva {reservaId}: cobro repetido, se devuelve el resultado original");
                return new PaymentResult
                {
                    Exitoso = pagoExistente.Aprobado,
                    CodigoTransferencia = pagoExistente.CodigoTransferencia,
                    Mensaje = pagoExistente.Aprobado ? "Pago aprobado (ya registrado)." : "Pago rechazado (ya registrado)."
                };
            }

            var resultadoPasarela = await _gateway.CobrarAsync(reservaId, montoCentavos, simularRechazo);

            // Se guardan también los pagos rechazados, para que un reintento reciba el mismo resultado.
            var pago = new PayModel
            {
                SolicitudId = solicitudId,
                ReservaId = reservaId,
                Aprobado = resultadoPasarela.Exitoso,
                MontoCentavos = montoCentavos,
                CodigoTransferencia = resultadoPasarela.CodigoTransferencia,
                FechaTransaccion = DateTime.UtcNow
            };
            await _repository.AgregarAsync(pago);

            var resultado = pago.Aprobado ? "aprobado" : "rechazado";
            _logger.LogInformation("[AUDITORIA] FIN Payment - Solicitud {SolicitudId}, reserva {ReservaId}: pago {Resultado}", solicitudId, reservaId, resultado);
            await _auditLogger.LogAsync($"[AUDITORIA] FIN Payment - Solicitud {solicitudId}, reserva {reservaId}: pago {resultado}");

            return new PaymentResult
            {
                Exitoso = resultadoPasarela.Exitoso,
                CodigoTransferencia = resultadoPasarela.CodigoTransferencia,
                Mensaje = resultadoPasarela.Mensaje
            };
        }
    }
}