using Availability.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Availability.Application.Interfaces
{
    public interface IAsientoRepository
    {
        // Obtiene solamente los asientos libres de un evento.
        Task<List<Asiento>> ObtenerDisponibles(int eventoId);

        // Obtiene todos los asientos de un evento, sin importar su estado.
        Task<List<Asiento>> ObtenerTodosPorEvento(int eventoId);

        // Busca un asiento por su identificador.
        Task<Asiento> ObtenerPorId(int asientoId);

        // Permite actualizar el estado de un asiento.
        Task Actualizar(Asiento asiento);
    }
}
