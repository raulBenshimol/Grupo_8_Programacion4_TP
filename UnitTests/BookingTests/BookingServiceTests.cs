using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Booking.Booking.Business;
using Booking.Booking.Business.Interfaces;
using Booking.Booking.Entities;
using Booking.Booking.Entities.Booking.API.DTOs;
using Booking.Booking.Entities.Booking.Entities.Enums;
using Booking.Booking.Entities.Interfaces;
using Xunit;

namespace UnitTests.Booking
{
    // Implementación sencilla de configuración para proporcionar valores en las pruebas.
    public class TestConfiguration : IConfiguration
    {
        private readonly Dictionary<string, string> _values = new();

        public TestConfiguration()
        {
            _values["Payment:OmitirCobro"] = "false";
            _values["Reservas:Monto"] = "100";
            _values["Reservas:MinutosRetencion"] = "15";
        }

        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var value) ? value : null;
            set { if (value != null) _values[key] = value; }
        }

        public IEnumerable<IConfigurationSection> GetChildren() => throw new NotImplementedException();
        public IChangeToken GetReloadToken()
        {
            // Devolver un token inactivo, ya que la configuración de prueba no cambia.
            return new DummyChangeToken();
        }

        private class DummyChangeToken : IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new DummyDisposable();

            private class DummyDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }
        public IConfigurationSection GetSection(string key)
        {
            // Devolver una sección que contiene el valor asociado a la clave solicitada.
            if (_values.TryGetValue(key, out var value))
            {
                return new ConfigurationSection(value);
            }
            return new ConfigurationSection(null);
        }

        private class ConfigurationSection : IConfigurationSection
        {
            private readonly string? _value;

            public ConfigurationSection(string? value)
            {
                _value = value;
            }

            public string? this[string key]
            {
                get => null;
                set { }
            }

            public string? Value
            {
                get => _value;
                set { }
            }

            public string Key => "";
            public string Path => "";
            public IEnumerable<IConfigurationSection> GetChildren() => throw new NotImplementedException();
            public IChangeToken GetReloadToken() => throw new NotImplementedException();
            public IConfigurationSection GetSection(string key) => throw new NotImplementedException();
        }
    }

    public class BookingServiceTests
    {
        private readonly Mock<IReservaRepository> _repositoryMock;
        private readonly Mock<IPaymentClient> _paymentClientMock;
        private readonly TestConfiguration _configurationMock;
        private readonly Mock<ILogger<BookingService>> _loggerMock;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            _repositoryMock = new Mock<IReservaRepository>();
            _paymentClientMock = new Mock<IPaymentClient>();
            _configurationMock = new TestConfiguration();
            _loggerMock = new Mock<ILogger<BookingService>>();

            _bookingService = new BookingService(
                _repositoryMock.Object,
                _paymentClientMock.Object,
                _configurationMock,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetReservationStatusBySolicitudId_WhenReservationExists_ReturnsBookingResponseDto()
        {
            // Arrange: preparar una reserva confirmada y configurar el repositorio para encontrarla.
            var solicitudId = Guid.NewGuid();
            var reserva = new Reserva
            {
                Id = Guid.NewGuid(),
                SolicitudId = solicitudId,
                Estado = ReservationStatusEnum.Confirmada,
                MotivoRechazo = null
            };

            _repositoryMock
                .Setup(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reserva);

            // Act: consultar el estado de la reserva usando el identificador de solicitud.
            var result = await _bookingService.GetReservationStatusBySolicitudId(solicitudId);

            // Assert: comprobar el mapeo de identificador, estado y motivo de rechazo.
            Assert.NotNull(result);
            Assert.Equal(reserva.Id, result.id);
            Assert.Equal(ReservationStatusEnum.Confirmada, result.status);
            Assert.Null(result.RejectionMotive);

            _repositoryMock.Verify(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetReservationStatusBySolicitudId_WhenReservationDoesNotExist_ReturnsNull()
        {
            // Arrange: configurar el repositorio para indicar que no existe una reserva.
            var solicitudId = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reserva?)null);

            // Act: consultar el estado de una solicitud sin reserva asociada.
            var result = await _bookingService.GetReservationStatusBySolicitudId(solicitudId);

            // Assert: comprobar que el servicio devuelve null y consulta el repositorio una vez.
            Assert.Null(result);

            _repositoryMock.Verify(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessReservation_WhenNewReservation_CreatesAndProcessesSuccessfully()
        {
            // Arrange: configurar una solicitud nueva, el guardado y un pago aprobado.
            var solicitudId = Guid.NewGuid();
            var eventoId = 1;
            var seatId = 10;
            var userId = 1;
            var monto = 100m;

            _repositoryMock
                .Setup(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reserva?)null);

            _repositoryMock
                .Setup(r => r.Guardar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _paymentClientMock
                .Setup(p => p.ProcesarPago(It.IsAny<Guid>(), It.IsAny<Guid>(), monto))
                .ReturnsAsync(ResultadoPago.Aprobado);

            _repositoryMock
                .Setup(r => r.Actualizar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act: procesar la reserva completa.
            var result = await _bookingService.ProcessReservation(solicitudId, eventoId, seatId, userId);

            // Assert: comprobar que queda confirmada y que se guardó, cobró y actualizó.
            Assert.NotNull(result);
            Assert.Equal(ReservationStatusEnum.Confirmada, result.status);
            Assert.Null(result.RejectionMotive);

            _repositoryMock.Verify(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.Guardar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()), Times.Once);
            _paymentClientMock.Verify(p => p.ProcesarPago(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Once);
            _repositoryMock.Verify(r => r.Actualizar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessReservation_WhenDuplicateReservation_ReturnsExistingResult()
        {
            // Arrange: preparar una reserva ya confirmada para una solicitud repetida.
            var solicitudId = Guid.NewGuid();
            var eventoId = 1;
            var seatId = 10;
            var userId = 1;

            var existingReserva = new Reserva
            {
                Id = Guid.NewGuid(),
                SolicitudId = solicitudId,
                Estado = ReservationStatusEnum.Confirmada,
                MotivoRechazo = null
            };

            _repositoryMock
                .Setup(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingReserva);

            // Act: procesar de nuevo la solicitud existente.
            var result = await _bookingService.ProcessReservation(solicitudId, eventoId, seatId, userId);

            // Assert: comprobar que se devuelve la reserva existente sin cambiar su estado.
            Assert.NotNull(result);
            Assert.Equal(ReservationStatusEnum.Confirmada, result.status);

            // Assert: verificar que el reintento no vuelve a solicitar un cobro.
            _paymentClientMock.Verify(p => p.ProcesarPago(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task ProcessReservation_WhenPaymentFails_SetsStatusToRejected()
        {
            // Arrange: configurar una reserva nueva cuyo pago será rechazado.
            var solicitudId = Guid.NewGuid();
            var eventoId = 1;
            var seatId = 10;
            var userId = 1;
            var monto = 100m;

            _repositoryMock
                .Setup(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reserva?)null);

            _repositoryMock
                .Setup(r => r.Guardar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _paymentClientMock
                .Setup(p => p.ProcesarPago(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>()))
                .ReturnsAsync(ResultadoPago.Rechazado);

            _repositoryMock
                .Setup(r => r.Actualizar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act: procesar la reserva con resultado de pago fallido.
            var result = await _bookingService.ProcessReservation(solicitudId, eventoId, seatId, userId);

            // Assert: comprobar el estado rechazado, el motivo y la actualización persistida.
            Assert.NotNull(result);
            Assert.Equal(ReservationStatusEnum.Rechazada, result.status);
            Assert.Equal(MotivosRechazo.PagoFallido, result.RejectionMotive);

            _repositoryMock.Verify(r => r.Actualizar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessReservation_WhenPaymentUnavailable_SetsStatusToRejected()
        {
            // Arrange: configurar una reserva nueva y simular que el servicio de pago no está disponible.
            var solicitudId = Guid.NewGuid();
            var eventoId = 1;
            var seatId = 10;
            var userId = 1;
            var monto = 100m;

            _repositoryMock
                .Setup(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reserva?)null);

            _repositoryMock
                .Setup(r => r.Guardar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _paymentClientMock
                .Setup(p => p.ProcesarPago(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>()))
                .ReturnsAsync(ResultadoPago.NoDisponible);

            _repositoryMock
                .Setup(r => r.Actualizar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act: procesar la reserva con el servicio de pago no disponible.
            var result = await _bookingService.ProcessReservation(solicitudId, eventoId, seatId, userId);

            // Assert: comprobar que la reserva se rechaza con el motivo correspondiente.
            Assert.NotNull(result);
            Assert.Equal(ReservationStatusEnum.Rechazada, result.status);
            Assert.Equal(MotivosRechazo.PagoNoDisponible, result.RejectionMotive);
        }

        [Fact]
        public async Task VencerReservasRetenidas_ReturnsVencedReservations()
        {
            // Arrange: preparar reservas vencidas devueltas por el repositorio.
            var vencidas = new List<Reserva>
            {
                new Reserva
                {
                    Id = Guid.NewGuid(),
                    SolicitudId = Guid.NewGuid(),
                    Estado = ReservationStatusEnum.Vencida,
                    MotivoRechazo = null
                },
                new Reserva
                {
                    Id = Guid.NewGuid(),
                    SolicitudId = Guid.NewGuid(),
                    Estado = ReservationStatusEnum.Vencida,
                    MotivoRechazo = null
                }
            };

            _repositoryMock
                .Setup(r => r.VencerRetenidas(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(vencidas);

            // Act: solicitar al servicio que venza las reservas retenidas.
            var result = await _bookingService.VencerReservasRetenidas();

            // Assert: comprobar la cantidad, el estado devuelto y la llamada al repositorio.
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(ReservationStatusEnum.Vencida, r.status));

            _repositoryMock.Verify(r => r.VencerRetenidas(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessReservation_WhenOmitirCobro_LeavesReservationInHoldStatus()
        {
            // Arrange: activar la opción de omitir el cobro y configurar una solicitud nueva.
            var solicitudId = Guid.NewGuid();
            var eventoId = 1;
            var seatId = 10;
            var userId = 1;

            _configurationMock["Payment:OmitirCobro"] = "true";

            _repositoryMock
                .Setup(r => r.ObtenerPorSolicitudId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reserva?)null);

            _repositoryMock
                .Setup(r => r.Guardar(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act: procesar la reserva con el cobro omitido.
            var result = await _bookingService.ProcessReservation(solicitudId, eventoId, seatId, userId);

            // Assert: comprobar que permanece retenida.
            Assert.NotNull(result);
            Assert.Equal(ReservationStatusEnum.Retenida, result.status);

            // Assert: verificar que no se solicitó ningún pago.
            _paymentClientMock.Verify(p => p.ProcesarPago(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
        }
    }
}
