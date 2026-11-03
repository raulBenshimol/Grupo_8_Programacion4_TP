//Bruno Scucimarri
using System.Text.Json;
using System.Text.Json.Serialization;
using Booking.Booking.Business.Interfaces;

namespace Booking.Booking.Infra.Messaging
{
    // Valida el formato real del mensaje. No usa SolicitudReserva.schema.json porque exige
    // eventoId como Guid y camelCase, y rechazaría lo que Availability publica hoy.
    public class SolicitudReservaValidator : IValidator<SolicitudReservaMensaje>
    {
        // Web: no distingue mayúsculas, porque Availability publica en PascalCase y el contrato
        // define camelCase. Strict: un número escrito como texto no es válido.
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.Strict
        };

        public IReadOnlyList<string> Validate(string contenido, out SolicitudReservaMensaje? resultado)
        {
            try
            {
                resultado = JsonSerializer.Deserialize<SolicitudReservaMensaje>(contenido, JsonOptions);
            }
            catch (JsonException ex)
            {
                resultado = null;
                return [ex.Message];
            }

            if (resultado is null)
            {
                return ["El cuerpo del mensaje es null."];
            }

            var errores = new List<string>();

            if (resultado.SolicitudId == Guid.Empty)
            {
                errores.Add("SolicitudId no puede ser un Guid vacío.");
            }
            if (resultado.EventoId < 1)
            {
                errores.Add("EventoId debe ser mayor o igual a 1.");
            }
            if (resultado.AsientoId < 1)
            {
                errores.Add("AsientoId debe ser mayor o igual a 1.");
            }
            if (resultado.UsuarioId < 1)
            {
                errores.Add("UsuarioId debe ser mayor o igual a 1.");
            }

            return errores;
        }
    }
}
