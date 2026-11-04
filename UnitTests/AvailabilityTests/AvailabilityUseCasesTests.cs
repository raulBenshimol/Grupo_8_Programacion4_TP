using Availability.Application.Interfaces;
using Availability.Application.UseCases;
using Availability.Domain.Entities;
using Availability.Domain.Enum;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Availability;

public class AvailabilityUseCasesTests
{
    [Fact]
    public async Task ConsultarDisponibilidad_Ejecutar_DevuelveDisponibles()
    {
        // Arrange: preparar el repositorio para devolver los asientos libres del evento 7.
        var repository = new Mock<IAsientoRepository>();
        var seats = new List<Asiento> { new() { Id = 1, EventoId = 7, Estado = EstadoAsiento.libre } };
        repository.Setup(r => r.ObtenerDisponibles(7)).ReturnsAsync(seats);
        var useCase = new ConsultarDisponibilidadUseCase(repository.Object, Mock.Of<ILogger<ConsultarDisponibilidadUseCase>>());

        // Act: consultar la disponibilidad del evento.
        var result = await useCase.Ejecutar(7);

        // Assert: comprobar la lista devuelta y que se consultó el repositorio correcto.
        Assert.Same(seats, result);
        repository.Verify(r => r.ObtenerDisponibles(7), Times.Once);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_EjecutarTodos_DevuelveTodosLosAsientos()
    {
        // Arrange: preparar asientos libres y ocupados para devolver el inventario completo.
        var repository = new Mock<IAsientoRepository>();
        var seats = new List<Asiento>
        {
            new() { Id = 1, EventoId = 7, Estado = EstadoAsiento.libre },
            new() { Id = 2, EventoId = 7, Estado = EstadoAsiento.ocupado }
        };
        repository.Setup(r => r.ObtenerTodosPorEvento(7)).ReturnsAsync(seats);
        var useCase = new ConsultarDisponibilidadUseCase(repository.Object, Mock.Of<ILogger<ConsultarDisponibilidadUseCase>>());

        // Act: solicitar todos los asientos del evento.
        var result = await useCase.EjecutarTodos(7);

        // Assert: comprobar que se devolvieron ambos estados y se consultó el repositorio.
        Assert.Same(seats, result);
        Assert.Equal(2, result.Count);
        repository.Verify(r => r.ObtenerTodosPorEvento(7), Times.Once);
    }

    [Fact]
    public async Task ObtenerResultadoSolicitud_DevuelveSolicitudExistente()
    {
        // Arrange: configurar el repositorio con una solicitud existente.
        var repository = new Mock<ISolicitudRepository>();
        var id = Guid.NewGuid();
        var solicitud = new SolicitudReserva { Id = id, Estado = EstadoSolicitud.Confirmada };
        repository.Setup(r => r.ObtenerPorId(id)).ReturnsAsync(solicitud);
        var useCase = new ObtenerResultadoSolicitudUseCase(repository.Object, Mock.Of<ILogger<ObtenerResultadoSolicitudUseCase>>());

        // Act: buscar la solicitud por su identificador.
        var result = await useCase.Ejecutar(id);

        // Assert: comprobar que se devuelve la misma solicitud encontrada.
        Assert.Same(solicitud, result);
    }

    [Fact]
    public async Task ObtenerResultadoSolicitud_DevuelveNullSiNoExiste()
    {
        // Arrange: configurar el repositorio para indicar que la solicitud no existe.
        var repository = new Mock<ISolicitudRepository>();
        var id = Guid.NewGuid();
        repository.Setup(r => r.ObtenerPorId(id)).ReturnsAsync((SolicitudReserva?)null);
        var useCase = new ObtenerResultadoSolicitudUseCase(repository.Object, Mock.Of<ILogger<ObtenerResultadoSolicitudUseCase>>());

        // Act: buscar la solicitud por su identificador.
        var result = await useCase.Ejecutar(id);

        // Assert: comprobar que el resultado es null.
        Assert.Null(result);
    }

    [Fact]
    public async Task SolicitarReserva_GuardaPublicaYPresentaLaSolicitud()
    {
        // Arrange: preparar los dobles y capturar la solicitud guardada y presentada.
        var repository = new Mock<ISolicitudRepository>();
        var broker = new Mock<IMessageBroker>();
        var output = new Mock<ISolicitudOutputPort>();
        SolicitudReserva? persisted = null;
        Guid presentedId = Guid.Empty;
        repository.Setup(r => r.Guardar(It.IsAny<SolicitudReserva>()))
            .Callback<SolicitudReserva>(s => persisted = s)
            .Returns(Task.CompletedTask);
        broker.Setup(b => b.PublicarSolicitud(It.IsAny<SolicitudReserva>())).Returns(Task.CompletedTask);
        output.Setup(o => o.PresentarResultado(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<Guid, string, string>((id, _, _) => presentedId = id)
            .Returns(Task.CompletedTask);
        var useCase = new SolicitarReservaInteractor(
            repository.Object, broker.Object, output.Object, Mock.Of<ILogger<SolicitarReservaInteractor>>());

        // Act: ejecutar el flujo de solicitud de reserva.
        await useCase.Ejecutar(12, 34, 56);

        // Assert: validar los datos de la solicitud y las llamadas a repositorio, broker y output port.
        Assert.NotNull(persisted);
        Assert.Equal(56, persisted.EventoId);
        Assert.Equal(12, persisted.AsientoId);
        Assert.Equal(34, persisted.UsuarioId);
        Assert.Equal(EstadoSolicitud.PendientePago, persisted.Estado);
        Assert.Equal(persisted.Id, presentedId);
        repository.Verify(r => r.Guardar(It.IsAny<SolicitudReserva>()), Times.Once);
        broker.Verify(b => b.PublicarSolicitud(It.IsAny<SolicitudReserva>()), Times.Once);
        output.Verify(o => o.PresentarResultado(It.IsAny<Guid>(), "Pendiente", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SolicitarReserva_SiGuardarFalla_PropagaExcepcionYNoPublica()
    {
        // Arrange: configurar el repositorio para que falle al guardar la solicitud.
        var repository = new Mock<ISolicitudRepository>();
        var broker = new Mock<IMessageBroker>();
        var output = new Mock<ISolicitudOutputPort>();
        repository.Setup(r => r.Guardar(It.IsAny<SolicitudReserva>()))
            .ThrowsAsync(new InvalidOperationException("Error de almacenamiento"));
        var useCase = new SolicitarReservaInteractor(
            repository.Object, broker.Object, output.Object, Mock.Of<ILogger<SolicitarReservaInteractor>>());

        // Act / Assert: ejecutar el caso de uso y comprobar que la excepción se propaga.
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.Ejecutar(1, 2, 3));

        // Assert: confirmar que, tras el fallo al guardar, no se publica ni se presenta la solicitud.
        broker.Verify(b => b.PublicarSolicitud(It.IsAny<SolicitudReserva>()), Times.Never);
        output.Verify(o => o.PresentarResultado(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("retenida", EstadoSolicitud.Reservado, EstadoAsiento.reservado)]
    [InlineData("confirmada", EstadoSolicitud.Confirmada, EstadoAsiento.ocupado)]
    [InlineData("rechazada", EstadoSolicitud.Rechazada, null)]
    [InlineData("vencida", EstadoSolicitud.Vencida, null)]
    public async Task SincronizarEstadoReserva_MapeaEstadoBooking(string bookingState, EstadoSolicitud expectedState, EstadoAsiento? expectedSeatState)
    {
        // Arrange: configurar el mock para aceptar el estado de Booking y su estado de asiento esperado.
        var repository = new Mock<ISolicitudRepository>();
        var id = Guid.NewGuid();
        repository.Setup(r => r.SincronizarAsiento(id, expectedState, expectedSeatState, It.IsAny<MotivoRechazo?>())).ReturnsAsync(true);
        var useCase = new SincronizarEstadoReservaUseCase(repository.Object, Mock.Of<ILogger<SincronizarEstadoReservaUseCase>>());

        // Act: sincronizar el estado recibido desde Booking.
        await useCase.Ejecutar(id, bookingState);

        // Assert: comprobar que se envían al repositorio los estados traducidos.
        repository.Verify(r => r.SincronizarAsiento(id, expectedState, expectedSeatState, It.IsAny<MotivoRechazo?>()), Times.Once);
    }

    [Theory]
    [InlineData("asientoocupado", MotivoRechazo.Ocupado)]
    [InlineData("pagofallido", MotivoRechazo.PagoFallido)]
    [InlineData("pagonodisponible", MotivoRechazo.PagoNoDisponible)]
    public async Task SincronizarEstadoReserva_MapeaMotivoRechazo(string bookingReason, MotivoRechazo expectedReason)
    {
        // Arrange: preparar el motivo de Booking y el motivo de rechazo esperado.
        var repository = new Mock<ISolicitudRepository>();
        var id = Guid.NewGuid();
        repository.Setup(r => r.SincronizarAsiento(id, EstadoSolicitud.Rechazada, null, expectedReason)).ReturnsAsync(true);
        var useCase = new SincronizarEstadoReservaUseCase(repository.Object, Mock.Of<ILogger<SincronizarEstadoReservaUseCase>>());

        // Act: sincronizar la reserva rechazada con su motivo.
        await useCase.Ejecutar(id, "rechazada", bookingReason);

        // Assert: comprobar que el motivo se traduce correctamente al dominio.
        repository.Verify(r => r.SincronizarAsiento(id, EstadoSolicitud.Rechazada, null, expectedReason), Times.Once);
    }

    [Theory]
    [InlineData("estado-desconocido", false)]
    [InlineData("", false)]
    [InlineData("confirmada", true)]
    public async Task SincronizarEstadoReserva_IgnoraEstadosInvalidosOIdVacio(string state, bool useEmptyId)
    {
        // Arrange: preparar un estado desconocido o vacío, o un identificador vacío según el caso.
        var repository = new Mock<ISolicitudRepository>();
        var useCase = new SincronizarEstadoReservaUseCase(repository.Object, Mock.Of<ILogger<SincronizarEstadoReservaUseCase>>());

        // Act: intentar sincronizar los datos recibidos.
        await useCase.Ejecutar(useEmptyId ? Guid.Empty : Guid.NewGuid(), state);

        // Assert: comprobar que los datos inválidos no provocan una actualización del repositorio.
        repository.Verify(r => r.SincronizarAsiento(
            It.IsAny<Guid>(), It.IsAny<EstadoSolicitud>(), It.IsAny<EstadoAsiento?>(), It.IsAny<MotivoRechazo?>()), Times.Never);
    }
}
