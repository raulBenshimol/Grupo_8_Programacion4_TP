using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payment.Grpc.Data
{
    public class PayDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
    {  
        public PaymentDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("PaymentDb") ?? "Data Source=payments.db";

            return new PaymentDbContext(new DbContextOptionsBuilder<PaymentDbContext>()
                .UseSqlite(connectionString)
                .Options);
        }
    }

}
