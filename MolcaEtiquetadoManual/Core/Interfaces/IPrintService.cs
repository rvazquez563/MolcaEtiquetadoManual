using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Core/Interfaces/IPrintService.cs
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    public interface IPrintService
    {
        bool ImprimirEtiqueta(OrdenProduccion orden, string codigoBarras);
    }
}