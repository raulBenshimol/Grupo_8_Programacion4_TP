using Microsoft.EntityFrameworkCore;
using Payment.Grpc.Models;

namespace Payment.Grpc.Data

{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<PayModel> Pagos => Set<PayModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var pago = modelBuilder.Entity<PayModel>();
            pago.ToTable("Pagos");
            pago.HasKey(p => p.Id);
            //pago.Property(p => p.Id).UseAutoIncrement();
            pago.Property(p => p.SolicitudId).IsRequired();
            pago.Property(p => p.ReservaId).IsRequired();
            pago.Property(p => p.CodigoTransferencia).HasMaxLength(64);
            pago.Property(p => p.FechaTransaccion).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd(); // Valor por defecto en la base de datos al insertar
            // Idempotencia: cada reserva se cobra una sola vez. Si Booking reintenta el cobro
            // (por ejemplo, porque se le venció el tiempo de espera), la base rechaza un segundo
            // registro con la misma ReservaId. Business lo verifica antes; esto es la red de seguridad.
            pago.HasIndex(p => p.ReservaId)
                .IsUnique()
                .HasDatabaseName("UX_Pagos_ReservaId");
        }
    }
}
