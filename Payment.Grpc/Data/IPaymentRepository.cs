using Payment.Grpc.Models;

namespace Payment.Grpc.Data
{
    // Operaciones de datos que necesita Business. Business depende de esta interfaz y no de EF Core:
    // no sabe si los pagos se guardan en SQLite, en otro motor o en memoria.
    public interface IPaymentRepository
    {
        Task<PayModel?> ObtenerPorReservaAsync(Guid reservaId); // Devuelve el pago registrado para esa reserva, o null si todavía no se cobró (idempotencia).

        Task AgregarAsync(PayModel pago); // Guarda un pago nuevo.

    }
}
