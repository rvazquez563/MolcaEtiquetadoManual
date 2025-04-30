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
        // Imprime una etiqueta enviando los comandos ZPL a la impresora real
        bool ImprimirEtiqueta(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras);

        // Simula una impresión (para pruebas)
        bool SimularImpresion(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras);
    }
}