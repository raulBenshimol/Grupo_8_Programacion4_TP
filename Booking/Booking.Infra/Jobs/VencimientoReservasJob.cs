//Bruno Scucimarri
using Booking.Booking.Entities.Booking.Business.Interfaces;

namespace Booking.Booking.Infra.Jobs
{
    // Job periódico con el mecanismo nativo de .NET: un BackgroundService con PeriodicTimer.
    // Corre una vez al arrancar y después cada minuto, dentro del proceso de Booking.
    public class VencimientoReservasJob : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VencimientoReservasJob> _logger;

        public VencimientoReservasJob(IServiceScopeFactory scopeFactory, ILogger<VencimientoReservasJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // PeriodicTimer no superpone ejecuciones: si una tarda más de un minuto, el siguiente tick espera.
            using var timer = new PeriodicTimer(Intervalo);

            try
            {
                do
                {
                    await VencerRetenidas(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Apagado normal de la aplicación.
            }
        }

        private async Task VencerRetenidas(CancellationToken ct)
        {
            try
            {
                // BookingService y el DbContext son scoped: el job es singleton y necesita un scope por ejecución.
                using var scope = _scopeFactory.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                await bookingService.VencerReservasRetenidas(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Un error no detiene el job: se reintenta en el próximo minuto.
                _logger.LogError(ex, "Error al vencer las reservas retenidas.");
            }
        }
    }
}
