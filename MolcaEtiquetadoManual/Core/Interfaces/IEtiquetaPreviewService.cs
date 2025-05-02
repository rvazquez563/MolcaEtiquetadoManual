using System.Windows.Media.Imaging;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    public interface IEtiquetaPreviewService
    {
        /// <summary>
        /// Genera una vista previa de la etiqueta
        /// </summary>
        EtiquetaPreview GenerarVistaPrevia(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras);

        /// <summary>
        /// Genera una imagen del código de barras para la vista previa
        /// </summary>
        BitmapSource GenerarImagenCodigoBarras(string contenido);
    }
}