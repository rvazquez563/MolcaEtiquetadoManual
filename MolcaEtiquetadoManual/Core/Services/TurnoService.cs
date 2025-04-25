using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Core/Services/TurnoService.cs
using System;
using MolcaEtiquetadoManual.Core.Interfaces;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class TurnoService:ITurnoService
    {
        public (string numeroTurno, DateTime fechaProduccion) ObtenerTurnoYFechaProduccion()
        {
            DateTime ahora = DateTime.Now;
            int horaActual = ahora.Hour;
            DateTime fechaProduccion = ahora;
            string turno;

            // Determinar el turno basado en la hora actual
            if (horaActual >= 6 && horaActual < 10)
            {
                turno = "1"; // Turno 1: 06:00 - 09:59
            }
            else if (horaActual >= 10 && horaActual < 14)
            {
                turno = "2"; // Turno 2: 10:00 - 13:59
            }
            else if (horaActual >= 14 && horaActual < 17)
            {
                turno = "3"; // Turno 3: 14:00 - 16:59
            }
            else if (horaActual >= 17 && horaActual < 22)
            {
                turno = "4"; // Turno 4: 17:00 - 21:59
            }
            else
            {
                turno = "5"; // Turno 5: 22:00 - 05:59

                // Si estamos entre medianoche y las 05:59, la fecha de producción es del día anterior
                if (horaActual >= 0 && horaActual < 6)
                {
                    fechaProduccion = fechaProduccion.AddDays(-1);
                }
            }

            return (turno, fechaProduccion);
        }

        public string ObtenerNumeroTransaccion(DateTime fechaProduccion)
        {
            // EDTN: Número transmisión (MMDD)
            return fechaProduccion.ToString("MMdd");
        }
    }
}