using Availability.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Availability.Application.Interfaces
{
    public interface ISolicitudRepository
    {
        Task Guardar(SolicitudReserva solicitud);
        Task<SolicitudReserva?> ObtenerPorId(Guid id);

        Task Actualizar(SolicitudReserva solicitud);

        // Actualiza la solicitud y el asiento en una misma operación.
        Task ActualizarConAsiento(
            SolicitudReserva solicitud,
            Asiento asiento);

    }
}
