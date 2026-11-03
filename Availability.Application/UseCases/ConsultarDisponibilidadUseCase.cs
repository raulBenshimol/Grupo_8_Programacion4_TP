using Availability.Application.Interfaces;
using Availability.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Availability.Application.UseCases;

public class ConsultarDisponibilidadUseCase
{
    private readonly IAsientoRepository _asientoRepo;
    private readonly ILogger<ConsultarDisponibilidadUseCase> _logger;

    public ConsultarDisponibilidadUseCase(
        IAsientoRepository asientoRepo,
        ILogger<ConsultarDisponibilidadUseCase> logger)
    {
        _asientoRepo = asientoRepo;
        _logger = logger;
    }

    // Consulta únicamente los asientos libres.
    // Se utiliza para comprobar la disponibilidad.
    public async Task<List<Asiento>> Ejecutar(int eventoId)
    {
        _logger.LogInformation(
            "[AUDITORIA] INICIO - Consulta disponibilidad. EventoId: {EventoId}",
            eventoId);

        var asientos = await _asientoRepo.ObtenerDisponibles(eventoId);

        _logger.LogInformation(
            "[AUDITORIA] FIN - Consulta disponibilidad. EventoId: {EventoId}, Cantidad: {Cantidad}",
            eventoId, asientos.Count);

        return asientos;
    }

    // Consulta todos los asientos del evento, incluidos los ocupados.
    // Se utilizará para mostrar sus estados en la interfaz MVC.
    public async Task<List<Asiento>> EjecutarTodos(int eventoId)
    {
        _logger.LogInformation(
            "[AUDITORIA] INICIO - Consulta de todos los asientos. EventoId: {EventoId}",
            eventoId);

        var asientos = await _asientoRepo.ObtenerTodosPorEvento(eventoId);

        _logger.LogInformation(
            "[AUDITORIA] FIN - Consulta de todos los asientos. EventoId: {EventoId}, Cantidad: {Cantidad}",
            eventoId, asientos.Count);

        return asientos;
    }
}
