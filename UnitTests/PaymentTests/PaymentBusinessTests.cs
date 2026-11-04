using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Payment.Grpc.Business;
using Payment.Grpc.Business.Gateway;
using Payment.Grpc.Data;
using Payment.Grpc.Models;
using Xunit;

namespace UnitTests.Payment
{
    public class PaymentBusinessTests : IDisposable
    {
        private readonly Mock<IPaymentRepository> _repositoryMock;
        private readonly Mock<IPaymentGateway> _gatewayMock;
        private readonly Mock<ILogger<PaymentBusiness>> _loggerMock;
        private readonly PaymentBusiness _paymentBusiness;
        private readonly string _contentRootPath;

        public PaymentBusinessTests()
        {
            _repositoryMock = new Mock<IPaymentRepository>();
            _gatewayMock = new Mock<IPaymentGateway>();
            _loggerMock = new Mock<ILogger<PaymentBusiness>>();

            // Arrange: crear un entorno temporal para que la auditoría no escriba en datos del proyecto.
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            _contentRootPath = Path.Combine(Path.GetTempPath(), $"payment-audit-test-{Guid.NewGuid()}");
            mockEnvironment.Setup(e => e.ContentRootPath).Returns(_contentRootPath);

            var auditLogger = new PaymentAuditLogger(mockEnvironment.Object);

            _paymentBusiness = new PaymentBusiness(
                _repositoryMock.Object,
                _gatewayMock.Object,
                _loggerMock.Object,
                auditLogger);
        }

        public void Dispose()
        {
            if (Directory.Exists(_contentRootPath))
            {
                Directory.Delete(_contentRootPath, recursive: true);
            }
        }

        [Fact]
        public async Task ProcesarPagoAsync_WhenPaymentSucceeds_ReturnsSuccessfulResult()
        {
            // Arrange: configurar la pasarela para aprobar el pago y el repositorio sin pago previo.
            var solicitudId = Guid.NewGuid();
            var reservaId = Guid.NewGuid();
            long montoCentavos = 10000;
            var codigoTransferencia = "TRX123456";

            var gatewayResult = new GatewayResult
            {
                Exitoso = true,
                CodigoTransferencia = codigoTransferencia,
                Mensaje = "Pago aprobado"
            };

            _repositoryMock
                .Setup(r => r.ObtenerPorReservaAsync(reservaId))
                .ReturnsAsync((PayModel?)null);

            _gatewayMock
                .Setup(g => g.CobrarAsync(reservaId, montoCentavos, false))
                .ReturnsAsync(gatewayResult);

            _repositoryMock
                .Setup(r => r.AgregarAsync(It.IsAny<PayModel>()))
                .Returns(Task.CompletedTask);

            // Act: procesar el pago de la reserva.
            var result = await _paymentBusiness.ProcesarPagoAsync(solicitudId, reservaId, montoCentavos, false);

            // Assert: comprobar el resultado aprobado y las llamadas a repositorio y pasarela.
            Assert.True(result.Exitoso);
            Assert.Equal(codigoTransferencia, result.CodigoTransferencia);
            Assert.Equal("Pago aprobado", result.Mensaje);

            _repositoryMock.Verify(r => r.ObtenerPorReservaAsync(reservaId), Times.Once);
            _gatewayMock.Verify(g => g.CobrarAsync(reservaId, montoCentavos, false), Times.Once);
            _repositoryMock.Verify(r => r.AgregarAsync(It.IsAny<PayModel>()), Times.Once);
        }

        [Fact]
        public async Task ProcesarPagoAsync_WhenPaymentFails_ReturnsFailedResult()
        {
            // Arrange: configurar la pasarela para rechazar el pago y el repositorio sin pago previo.
            var solicitudId = Guid.NewGuid();
            var reservaId = Guid.NewGuid();
            long montoCentavos = 10000;

            var gatewayResult = new GatewayResult
            {
                Exitoso = false,
                CodigoTransferencia = string.Empty,
                Mensaje = "Fondos insuficientes"
            };

            _repositoryMock
                .Setup(r => r.ObtenerPorReservaAsync(reservaId))
                .ReturnsAsync((PayModel?)null);

            _gatewayMock
                .Setup(g => g.CobrarAsync(reservaId, montoCentavos, false))
                .ReturnsAsync(gatewayResult);

            _repositoryMock
                .Setup(r => r.AgregarAsync(It.IsAny<PayModel>()))
                .Returns(Task.CompletedTask);

            // Act: procesar el pago que será rechazado.
            var result = await _paymentBusiness.ProcesarPagoAsync(solicitudId, reservaId, montoCentavos, false);

            // Assert: comprobar el resultado fallido y que el intento queda registrado.
            Assert.False(result.Exitoso);
            Assert.Equal("Fondos insuficientes", result.Mensaje);

            _repositoryMock.Verify(r => r.ObtenerPorReservaAsync(reservaId), Times.Once);
            _gatewayMock.Verify(g => g.CobrarAsync(reservaId, montoCentavos, false), Times.Once);
            _repositoryMock.Verify(r => r.AgregarAsync(It.IsAny<PayModel>()), Times.Once);
        }

        [Fact]
        public async Task ProcesarPagoAsync_WhenPaymentAlreadyExists_ReturnsIdempotentResult()
        {
            // Arrange: registrar en el mock un pago previo para la misma reserva.
            var solicitudId = Guid.NewGuid();
            var reservaId = Guid.NewGuid();
            long montoCentavos = 10000;
            var codigoTransferencia = "TRX789012";

            var existingPayment = new PayModel
            {
                SolicitudId = solicitudId,
                ReservaId = reservaId,
                Aprobado = true,
                MontoCentavos = montoCentavos,
                CodigoTransferencia = codigoTransferencia,
                FechaTransaccion = DateTime.UtcNow
            };

            _repositoryMock
                .Setup(r => r.ObtenerPorReservaAsync(reservaId))
                .ReturnsAsync(existingPayment);

            // Act: volver a procesar el pago de esa reserva.
            var result = await _paymentBusiness.ProcesarPagoAsync(solicitudId, reservaId, montoCentavos, false);

            // Assert: comprobar que se devuelve el resultado original y no se repite el cobro.
            Assert.True(result.Exitoso);
            Assert.Equal(codigoTransferencia, result.CodigoTransferencia);
            Assert.Contains("ya registrado", result.Mensaje);

            // Assert: verificar que la pasarela y el guardado no se ejecutaron de nuevo.
            _repositoryMock.Verify(r => r.ObtenerPorReservaAsync(reservaId), Times.Once);
            _gatewayMock.Verify(g => g.CobrarAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<bool>()), Times.Never);
            _repositoryMock.Verify(r => r.AgregarAsync(It.IsAny<PayModel>()), Times.Never);
        }

        [Fact]
        public async Task ProcesarPagoAsync_WithSimulatedRejection_StoresAndReturnsFailedPayment()
        {
            // Arrange: configurar el rechazo simulado y el repositorio sin pago previo.
            var solicitudId = Guid.NewGuid();
            var reservaId = Guid.NewGuid();
            long montoCentavos = 10000;

            var gatewayResult = new GatewayResult
            {
                Exitoso = false,
                CodigoTransferencia = string.Empty,
                Mensaje = "Pago simulado rechazado"
            };

            _repositoryMock
                .Setup(r => r.ObtenerPorReservaAsync(reservaId))
                .ReturnsAsync((PayModel?)null);

            _gatewayMock
                .Setup(g => g.CobrarAsync(reservaId, montoCentavos, true))
                .ReturnsAsync(gatewayResult);

            _repositoryMock
                .Setup(r => r.AgregarAsync(It.IsAny<PayModel>()))
                .Returns(Task.CompletedTask);

            // Act: procesar el pago indicando que se simule un rechazo.
            var result = await _paymentBusiness.ProcesarPagoAsync(solicitudId, reservaId, montoCentavos, true);

            // Assert: comprobar el rechazo y que se guarda el pago rechazado.
            Assert.False(result.Exitoso);
            _repositoryMock.Verify(r => r.AgregarAsync(It.Is<PayModel>(p => p.ReservaId == reservaId && !p.Aprobado)), Times.Once);
        }

        [Fact]
        public async Task ProcesarPagoAsync_StoresPaymentWithCorrectData()
        {
            // Arrange: preparar los identificadores, el importe y la respuesta aprobada de la pasarela.
            var solicitudId = Guid.NewGuid();
            var reservaId = Guid.NewGuid();
            long montoCentavos = 15000;
            var codigoTransferencia = "TRX999888";

            var gatewayResult = new GatewayResult
            {
                Exitoso = true,
                CodigoTransferencia = codigoTransferencia,
                Mensaje = "Pago aprobado"
            };

            _repositoryMock
                .Setup(r => r.ObtenerPorReservaAsync(reservaId))
                .ReturnsAsync((PayModel?)null);

            _gatewayMock
                .Setup(g => g.CobrarAsync(reservaId, montoCentavos, false))
                .ReturnsAsync(gatewayResult);

            _repositoryMock
                .Setup(r => r.AgregarAsync(It.IsAny<PayModel>()))
                .Returns(Task.CompletedTask);

            // Act: procesar el pago para generar el registro correspondiente.
            await _paymentBusiness.ProcesarPagoAsync(solicitudId, reservaId, montoCentavos, false);

            // Assert: verificar que el modelo guardado contiene los datos de solicitud, reserva e importe esperados.
            _repositoryMock.Verify(r => r.AgregarAsync(It.Is<PayModel>(p =>
                p.SolicitudId == solicitudId &&
                p.ReservaId == reservaId &&
                p.Aprobado == true &&
                p.MontoCentavos == montoCentavos &&
                p.CodigoTransferencia == codigoTransferencia
            )), Times.Once);
        }
    }
}
