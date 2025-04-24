using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    // Core/Interfaces/IEtiquetadoService.cs
    public interface IEtiquetadoService
    {
        OrdenProduccion BuscarOrdenPorDun14(string dun14);
        void GuardarEtiqueta(EtiquetaGenerada etiqueta);
        int ObtenerSiguienteNumeroSecuencial();
        List<EtiquetaGenerada> ObtenerEtiquetasGeneradas(DateTime fechaInicio, DateTime fechaFin);
    }
}
