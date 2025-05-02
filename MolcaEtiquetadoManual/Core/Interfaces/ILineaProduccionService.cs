using MolcaEtiquetadoManual.Core.Models;
using System.Collections.Generic;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    public interface ILineaProduccionService
    {
        List<LineaProduccion> GetAllLineas();
        LineaProduccion GetLineaById(int id);
        void AddLinea(LineaProduccion linea);
        void UpdateLinea(LineaProduccion linea);
    }
}