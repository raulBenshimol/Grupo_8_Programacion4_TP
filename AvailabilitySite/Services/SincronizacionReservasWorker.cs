using Availability.Application.Interfaces;
using Availability.Controllers.Controllers;

namespace AvailabilitySite.Services;

public class SincronizacionReservasWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SincronizacionReservasWorker> _logger;

    public SincronizacionReservasWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SincronizacionReservasWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[AUDITORIA] Iniciado servicio de sincronización automática.");

        // Consultar periódicamente las solicitudes pendientes.
        using var temporizador = new PeriodicTimer(
            TimeSpan.FromSeconds(15));

        try
        {
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // Crear un scope para utilizar repositorios y DbContext.
                    using var scope = _scopeFactory.CreateScope();

                    var solicitudes = scope.ServiceProvider
                        .GetRequiredService<ISolicitudRepository>();

                    var seguimiento = scope.ServiceProvider
                        .GetRequiredService<SeguimientoController>();

                    var pendientes = await solicitudes.ObtenerPendientes();

                    foreach (var solicitud in pendientes)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        // Reutilizar el GET de Booking y el caso de uso
                        // de sincronización que ya probamos.
                        await seguimiento.ObtenerEstado(solicitud.Id);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[AUDITORIA] Error en sincronización automática.");
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Finalización normal al detener la aplicación.
        }

        _logger.LogInformation(
            "[AUDITORIA] Servicio de sincronización automática detenido.");
    }
}
