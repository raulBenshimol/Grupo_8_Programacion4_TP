using System.Text;

namespace Payment.Grpc.Data;

// Capa de Datos (N-Layer): registro de auditoría en un archivo de texto, además del log por consola.
// Escribir en un archivo es acceso a un recurso externo, como la base; por eso vive en Data y no en Services.
// Se registra como Singleton: el SemaphoreSlim solo evita escrituras simultáneas si todas las llamadas comparten esta instancia.
public sealed class PaymentAuditLogger
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PaymentAuditLogger(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");

        Directory.CreateDirectory(dataDirectory);

        _filePath = Path.Combine(dataDirectory, "payment-audit-log.txt");
    }

    public async Task LogAsync(string message, Exception? exception = null)
    {
        var entry = new StringBuilder()
            .AppendLine($"[{DateTimeOffset.UtcNow:O}] {message}");

        if (exception is not null)
        {
            entry.AppendLine($"Exception: {exception.Message}");
            entry.AppendLine($"StackTrace: {exception.StackTrace}");
        }

        await _lock.WaitAsync();

        try
        {
            await File.AppendAllTextAsync(
                _filePath,
                entry.AppendLine().ToString());
        }
        finally
        {
            _lock.Release();
        }
    }
}