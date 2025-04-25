using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    public interface ITurnoService
    {
        (string numeroTurno, DateTime fechaProduccion) ObtenerTurnoYFechaProduccion();
        string ObtenerNumeroTransaccion(DateTime fechaProduccion);
    }
}