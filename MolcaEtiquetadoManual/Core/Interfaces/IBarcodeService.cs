// Core/Interfaces/IBarcodeService.cs
using System.Windows.Media.Imaging;
using MolcaEtiquetadoManual.Core.Models;
using ZXing;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de generación y verificación de códigos de barras
    /// </summary>
    public interface IBarcodeService
    {
        /// <summary>
        /// Genera el código de barras completo para una etiqueta según la especificación
        /// </summary>
        string GenerarCodigoBarras(OrdenProduccion orden, EtiquetaGenerada etiqueta);

        /// <summary>
        /// Genera un código de barras vertical que contiene el LOTN (Lote)
        /// </summary>
        string GenerarCodigoBarrasVertical(EtiquetaGenerada etiqueta);

        /// <summary>
        /// Genera un código de barras horizontal que contiene la fecha y número secuencial
        /// </summary>
        string GenerarCodigoBarrasHorizontal(EtiquetaGenerada etiqueta);

        /// <summary>
        /// Verifica si un código de barras escaneado coincide con el esperado
        /// </summary>
        bool VerificarCodigoBarras(string codigoEsperado, string codigoEscaneado);

        /// <summary>
        /// Genera una imagen BitmapSource del código de barras para mostrar en la interfaz
        /// </summary>
        BitmapSource GenerarImagenCodigoBarras(string contenido, BarcodeFormat formato = BarcodeFormat.CODE_128,
            int ancho = 300, int alto = 100);

        /// <summary>
        /// Guarda una imagen del código de barras en un archivo
        /// </summary>
        string GuardarImagenCodigoBarras(string contenido, string rutaArchivo,
            BarcodeFormat formato = BarcodeFormat.CODE_128, int ancho = 300, int alto = 100);
    }
}