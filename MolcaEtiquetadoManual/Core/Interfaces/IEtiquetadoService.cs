using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    // Core/Interfaces/IEtiquetadoService.cs
    public interface IEtiquetadoService
    {
        OrdenProduccion BuscarOrdenPorDun14(string dun14);
        void GuardarEtiqueta(EtiquetaGenerada etiqueta);
        int ObtenerSiguienteNumeroSecuencialdeldia(string diajuliano);
        int ObtenerSiguienteNumeroSecuencial(string programaProduccion);
        List<EtiquetaGenerada> ObtenerEtiquetasGeneradas(DateTime fechaInicio, DateTime fechaFin);
        int GuardarEtiquetaConStoredProcedure(EtiquetaGenerada etiqueta);
        int ObtenerSiguienteNumeroPallet(string programaProduccion);

    }
}
