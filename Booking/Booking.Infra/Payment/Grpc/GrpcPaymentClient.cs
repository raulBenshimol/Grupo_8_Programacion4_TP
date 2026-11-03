//Bruno Scucimarri
using System.Diagnostics;
using Booking.Booking.Business.Interfaces;
using Grpc.Core;
using SistemaReservas.Contracts.Payment.V1;

namespace Booking.Booking.Infra.Payment.Grpc
{
    public class GrpcPaymentClient : IPaymentClient
    {
        private readonly PaymentService.PaymentServiceClient _client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GrpcPaymentClient> _logger;

        public GrpcPaymentClient(
            PaymentService.PaymentServiceClient client,
            IConfiguration configuration,
            ILogger<GrpcPaymentClient> logger)
        {
            _client = client;
            _configuration = configuration;
            _logger = logger;
        }

        // Pago aprobado o rechazado llega como respuesta normal. Si Payment o AuthServer no responden
        // es "no disponible". Cualquier otro error es de configuración o de datos, y se propaga.
        // Reintentar es seguro: Payment no cobra dos veces el mismo reserva_id.
        public async Task<ResultadoPago> ProcesarPago(Guid solicitudId, Guid reservaId, decimal monto)
        {
            var request = new PaymentRequestDto
            {
                SolicitudId = solicitudId.ToString(),
                ReservaId = reservaId.ToString(),
                MontoCentavos = (long)Math.Round(monto * 100, MidpointRounding.AwayFromZero),
                SimularRechazo = _configuration.GetValue<bool>("Payment:SimularRechazo")
            };

            _logger.LogInformation("Inicio del cobro de la reserva {ReservaId} (solicitud {SolicitudId}): {MontoCentavos} centavos.",
                reservaId, solicitudId, request.MontoCentavos);
            var reloj = Stopwatch.StartNew();
            ResultadoPago? resultado = null;

            try
            {
                resultado = await EnviarConReintentos(request, reservaId);
                _logger.LogInformation("Resultado del cobro de la reserva {ReservaId}: {Resultado}.", reservaId, resultado);
                return resultado.Value;
            }
            finally
            {
                _logger.LogInformation("Fin del cobro de la reserva {ReservaId}: {Resultado} en {Milisegundos} ms.",
                    reservaId, resultado?.ToString() ?? "error", reloj.ElapsedMilliseconds);
            }
        }

        private async Task<ResultadoPago> EnviarConReintentos(PaymentRequestDto request, Guid reservaId)
        {
            int maxIntentos = _configuration.GetValue<int>("Payment:MaxIntentos");

            for (int intento = 1; ; intento++)
            {
                try
                {
                    // El deadline es por intento: uno compartido dejaría sin tiempo a los reintentos.
                    var deadline = DateTime.UtcNow.AddSeconds(_configuration.GetValue<int>("Payment:TimeoutSegundos"));

                    // El token lo agrega AddCallCredentials, registrado en Program.cs.
                    PaymentResponseDto respuesta = await _client.ProcesarPagoAsync(request, deadline: deadline);

                    return respuesta.Exitoso ? ResultadoPago.Aprobado : ResultadoPago.Rechazado;
                }
                catch (RpcException ex) when (EsTransitorio(ex) && intento < maxIntentos)
                {
                    _logger.LogWarning(ex, "Payment no respondió para la reserva {ReservaId} (intento {Intento} de {MaxIntentos}).",
                        reservaId, intento, maxIntentos);
                    await Task.Delay(TimeSpan.FromSeconds(intento));
                }
                catch (RpcException ex) when (EsTransitorio(ex))
                {
                    _logger.LogWarning(ex, "Payment no disponible para la reserva {ReservaId}.", reservaId);
                    return ResultadoPago.NoDisponible;
                }
                // Un fallo al pedir el token ocurre dentro de la llamada gRPC y llega envuelto en RpcException.
                catch (RpcException ex) when (ex.Status.DebugException is HttpRequestException { StatusCode: null })
                {
                    _logger.LogWarning(ex, "AuthServer no disponible para la reserva {ReservaId}.", reservaId);
                    return ResultadoPago.NoDisponible;
                }
            }
        }

        private static bool EsTransitorio(RpcException ex) =>
            ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded;
    }
}
