using Availability.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Availability.Domain.Entities
{
    public class Asiento
    {
        public int Id { get; set; }
        public int EventoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public EstadoAsiento Estado { get; set; }

        // Identifica la solicitud que tiene retenido el asiento.
        // Null significa que no hay una retención local asociada.
        public Guid? SolicitudReservaId { get; set; }
    }
}

