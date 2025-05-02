using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    // Core/Interfaces/IEtiquetadoService.cs
    public interface IEtiquetadoService
    {
        OrdenProduccion BuscarOrdenPorDun14(string dun14);
        string GuardarEtiqueta(EtiquetaGenerada etiqueta);
        int ObtenerSiguienteNumeroSecuencialdeldia(string diajuliano, int linea);
        int ObtenerSiguienteNumeroSecuencial(string programaProduccion,int linea);
        List<EtiquetaGenerada> ObtenerEtiquetasGeneradas(DateTime fechaInicio, DateTime fechaFin);



    }
}
