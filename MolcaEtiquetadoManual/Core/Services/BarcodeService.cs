// Core/Services/BarcodeService.cs
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using ZXing;

namespace MolcaEtiquetadoManual.Core.Services
{
    /// <summary>
    /// Implementación básica de IBarcodeService sin dependencias externas
    /// </summary>
    public class BarcodeService : IBarcodeService
    {
        private readonly ILogService _logService;

        public BarcodeService(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Genera el código de barras para la etiqueta según la especificación funcional
        /// </summary>
        /// <param name="orden">Orden de producción</param>
        /// <param name="etiqueta">Etiqueta generada</param>
        /// <returns>String con el código de barras generado</returns>
        public string GenerarCodigoBarras(OrdenProduccion orden, EtiquetaGenerada etiqueta)
        {
            try
            {
                // Según la especificación del documento:
                // ITM(8) + URDT(6) + SOQS(4) + EDDT(6) + TDAY(6) + LOTN(7)

                // Formato cada campo según especificación
                string numeroArticulo = orden.NumeroArticulo.PadLeft(8, '0'); // 8 caracteres
                string fechaVencimiento = etiqueta.URDT.ToString("ddMMyy");  // 6 caracteres
                string cantidadPallet = etiqueta.SOQS.ToString("0000"); // 4 caracteres (completar con ceros)
                string fechaDeclaracion = DateTime.Now.ToString("ddMMyy"); // 6 caracteres
                string horaDeclaracion = DateTime.Now.ToString("HHmmss"); // 6 caracteres
                string lote = etiqueta.LOTN.PadRight(7, '0'); // 7 caracteres (completar con ceros)

                // Concatenar según la estructura definida
                string codigoBarras = $"{numeroArticulo}{fechaVencimiento}{cantidadPallet}{fechaDeclaracion}{horaDeclaracion}{lote}";

                // Verificar la longitud total (debe ser 37 caracteres)
                if (codigoBarras.Length != 37)
                {
                    _logService.Warning("Longitud incorrecta del código de barras: {0} caracteres", codigoBarras.Length);

                    // Asegurar que tenga exactamente 37 caracteres
                    if (codigoBarras.Length < 37)
                    {
                        codigoBarras = codigoBarras.PadRight(37, '0');
                    }
                    else
                    {
                        codigoBarras = codigoBarras.Substring(0, 37);
                    }
                }

                _logService.Debug("Código de barras generado: {0}", codigoBarras);
                return codigoBarras;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al generar código de barras");
                throw new Exception("Error al generar el código de barras para la etiqueta", ex);
            }
        }

        /// <summary>
        /// Genera un código de barras vertical que contiene el LOTN (Lote)
        /// </summary>
        public string GenerarCodigoBarrasVertical(EtiquetaGenerada etiqueta)
        {
            return etiqueta.LOTN;
        }

        /// <summary>
        /// Genera un código de barras horizontal que contiene la fecha y número secuencial
        /// </summary>
        public string GenerarCodigoBarrasHorizontal(EtiquetaGenerada etiqueta)
        {
            // Formato: Fecha declaración + Secuencial
            return $"{etiqueta.EDDT}{etiqueta.SEC:0000}";
        }

        /// <summary>
        /// Verifica si un código de barras escaneado coincide con el esperado
        /// </summary>
        public bool VerificarCodigoBarras(string codigoEsperado, string codigoEscaneado)
        {
            if (string.IsNullOrEmpty(codigoEscaneado) || string.IsNullOrEmpty(codigoEsperado))
            {
                _logService.Warning("Verificación de código de barras con valores nulos o vacíos");
                return false;
            }

            // Limpiamos los códigos antes de comparar
            string esperado = codigoEsperado.Trim();
            string escaneado = codigoEscaneado.Trim();

            bool coinciden = esperado == escaneado;

            if (coinciden)
            {
                _logService.Information("Verificación de código de barras exitosa");
            }
            else
            {
                _logService.Warning("Verificación de código de barras fallida. Esperado: {0}, Escaneado: {1}",
                    esperado, escaneado);
            }

            return coinciden;
        }

        /// <summary>
        /// Genera una imagen BitmapSource del código de barras para mostrar en la interfaz
        /// Esta implementación devuelve null ya que se requiere ZXing para generar la imagen
        /// </summary>
        public BitmapSource GenerarImagenCodigoBarras(string contenido, BarcodeFormat formato = BarcodeFormat.CODE_128,
            int ancho = 300, int alto = 100)
        {
            // Este método requiere ZXing, por lo que se delega a ZXingBarcodeService
            _logService.Warning("GenerarImagenCodigoBarras no implementado en BarcodeService básico");
            return null;
        }

        /// <summary>
        /// Guarda una imagen del código de barras en un archivo
        /// Esta implementación devuelve null ya que se requiere ZXing para generar la imagen
        /// </summary>
        public string GuardarImagenCodigoBarras(string contenido, string rutaArchivo,
            BarcodeFormat formato = BarcodeFormat.CODE_128, int ancho = 300, int alto = 100)
        {
            // Este método requiere ZXing, por lo que se delega a ZXingBarcodeService
            _logService.Warning("GuardarImagenCodigoBarras no implementado en BarcodeService básico");
            return null;
        }
    }
}