using Microsoft.EntityFrameworkCore;
using Payment.Grpc.Models;

namespace Payment.Grpc.Data
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _db;

        public PaymentRepository(PaymentDbContext db)
        {
            _db = db;
        }

        public Task<PayModel?> ObtenerPorReservaAsync(Guid reservaId)
        {
            return _db.Pagos.FirstOrDefaultAsync(p => p.ReservaId == reservaId);
        }

        public async Task AgregarAsync(PayModel pago)
        {
            _db.Pagos.Add(pago);
            await _db.SaveChangesAsync();
        }
    }
}
