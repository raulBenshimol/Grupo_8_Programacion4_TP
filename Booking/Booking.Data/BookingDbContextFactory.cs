using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Booking.Booking.Data;

// Solo para dotnet ef (migraciones); la aplicación configura el DbContext por su cuenta.
public class BookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        // dotnet ef no ejecuta Program.cs: la configuración se lee a mano desde la raíz del proyecto.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("BookingDb") ?? "Data Source=booking.db";

        return new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite(connectionString)
            .Options);
    }
}
