using Microsoft.AspNetCore.Mvc.RazorPages;
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
            pago.Property(p => p.Monto).HasPrecision(18, 2);
            pago.Property(p => p.FechaTransaccion).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd(); // Valor por defecto en la base de datos al insertar
            // Idempotencia: Service Bus puede entregar el mismo mensaje dos veces; el segundo choca acá.
            pago.HasIndex(p => p.SolicitudId)
                .IsUnique()
                .HasDatabaseName("UX_Pagos_SolicitudId");
        }
    }
}
