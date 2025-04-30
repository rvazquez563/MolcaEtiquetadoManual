using System;
using System.Windows.Media.Imaging;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class EtiquetaPreviewService : IEtiquetaPreviewService
    {
        private readonly IBarcodeService _barcodeService;
        private readonly ILogService _logService;

        public EtiquetaPreviewService(IBarcodeService barcodeService, ILogService logService)
        {
            _barcodeService = barcodeService ?? throw new ArgumentNullException(nameof(barcodeService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public EtiquetaPreview GenerarVistaPrevia(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras)
        {
            try
            {
                _logService.Debug("Generando vista previa para etiqueta...");

                if (orden == null || etiqueta == null)
                {
                    _logService.Warning("No se puede generar vista previa, orden o etiqueta es nula");
                    return null;
                }

                var preview = new EtiquetaPreview(orden, etiqueta, codigoBarras);

                // Generar imagen del código de barras
                if (!string.IsNullOrEmpty(codigoBarras))
                {
                    _logService.Debug($"Generando imagen para código de barras: {codigoBarras}");
                    preview.ImagenCodigoBarras = GenerarImagenCodigoBarras(codigoBarras);
                }
                else
                {
                    _logService.Debug("No se generó imagen de código de barras (código vacío)");
                }

                _logService.Debug("Vista previa generada correctamente");
                return preview;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al generar vista previa de etiqueta");
                return null;
            }
        }

        public BitmapSource GenerarImagenCodigoBarras(string contenido)
        {
            if (string.IsNullOrEmpty(contenido))
            {
                _logService.Warning("No se puede generar imagen de código de barras, contenido vacío");
                return null;
            }

            try
            {
                // Utilizar el servicio existente de códigos de barras para generar la imagen
                var imagen = _barcodeService.GenerarImagenCodigoBarras(
                    contenido,
                    ZXing.BarcodeFormat.CODE_128,
                    300, // Ancho
                    80   // Alto
                );

                if (imagen == null)
                {
                    _logService.Warning("El servicio de códigos de barras devolvió una imagen nula");
                }

                return imagen;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al generar imagen del código de barras");
                return null;
            }
        }
    }
}