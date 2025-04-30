using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    // Interfaz para el servicio
    public interface IJulianDateService
    {
        string ConvertirAFechaJuliana(DateTime fecha);
        DateTime ConvertirDesdeFechaJuliana(string fechaJuliana);
        string ConvertirDesdeFechaCorta(string fechaCorta);
        string ConvertirAHoraJuliana(string hora);
        string ConvertirAHoraJuliana(DateTime fecha);
    }
}
