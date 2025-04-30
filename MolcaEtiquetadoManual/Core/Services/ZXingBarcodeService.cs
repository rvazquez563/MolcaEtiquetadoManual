using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace MolcaEtiquetadoManual.Core.Services
{
    /// <summary>
    /// Implementa IBarcodeService utilizando la biblioteca ZXing.Net para
    /// generar y verificar códigos de barras
    /// </summary>
    public class ZXingBarcodeService : IBarcodeService
    {
        private readonly ILogService _logService;
        private readonly IJulianDateService _julianDateService;

        public ZXingBarcodeService(ILogService logService, IJulianDateService julianDateService)
        {
            _logService = logService;
            _julianDateService = julianDateService;
        }

        /// <summary>
        /// Genera el código de barras para una etiqueta según la especificación
        /// </summary>
        public string GenerarCodigoBarras(OrdenProduccion orden, EtiquetaGenerada etiqueta)
        {
            try
            {
                _logService.Debug("Generando código de barras para etiqueta...");

                // Según la especificación del documento:
                // ITM(8) + URDT(6) + SOQS(4) + EDDT(6) + TDAY(6) + LOTN(7)

                // Formato de cada campo según la especificación
                string numeroArticulo = orden.NumeroArticulo.PadLeft(8, '0'); // 8 caracteres
                string fechaVencimiento = etiqueta.URDT.ToString("ddMMyy");  // 6 caracteres (DDMMAA)
                string cantidadPallet = etiqueta.SOQS.ToString("0000"); // 4 caracteres
                string fechaDeclaracion = DateTime.Now.ToString("ddMMyy"); // 6 caracteres (DDMMAA)
                string horaDeclaracion = DateTime.Now.ToString("HHmmss"); // 6 caracteres (HHMMSS)
                string lote = etiqueta.LOTN.PadRight(7, '0'); // 7 caracteres 

                _logService.Debug($"Componentes del código de barras: ITM={numeroArticulo}, URDT={fechaVencimiento}, SOQS={cantidadPallet}, EDDT={fechaDeclaracion}, TDAY={horaDeclaracion}, LOTN={lote}");

                // Concatenar según la estructura definida
                string codigoBarras = $"{numeroArticulo}{fechaVencimiento}{cantidadPallet}{fechaDeclaracion}{horaDeclaracion}{lote}";

                // Verificar la longitud total (debe ser 37 caracteres)
                if (codigoBarras.Length != 37)
                {
                    _logService.Warning($"Longitud incorrecta del código de barras: {codigoBarras.Length} caracteres, esperados 37");

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

                _logService.Debug($"Código de barras generado: {codigoBarras}");
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
                _logService.Warning($"Verificación de código de barras fallida. Esperado: {esperado}, Escaneado: {escaneado}");
            }

            return coinciden;
        }

        /// <summary>
        /// Genera una imagen BitmapSource del código de barras para mostrar en la interfaz
        /// </summary>
        public BitmapSource GenerarImagenCodigoBarras(string contenido, BarcodeFormat formato = BarcodeFormat.CODE_128,
            int ancho = 300, int alto = 100)
        {
            try
            {
                if (string.IsNullOrEmpty(contenido))
                {
                    _logService.Warning("No se puede generar imagen de código de barras, contenido vacío");
                    return null;
                }

                _logService.Debug($"Generando imagen de código de barras para: {contenido}");

                // Crear un escritor de códigos de barras ZXing
                var writer = new BarcodeWriterPixelData
                {
                    Format = formato,
                    Options = new EncodingOptions
                    {
                        Width = ancho,
                        Height = alto,
                        Margin = 5,
                        PureBarcode = false
                    }
                };

                try
                {
                    // Generar el código de barras como PixelData
                    var pixelData = writer.Write(contenido);
                    _logService.Debug($"Código de barras generado correctamente. Dimensiones: {pixelData.Width}x{pixelData.Height}");

                    // Crear BitmapSource desde PixelData
                    var bitmap = BitmapSource.Create(
                        pixelData.Width,
                        pixelData.Height,
                        96, 96,
                        PixelFormats.Bgr32,
                        null,
                        pixelData.Pixels,
                        pixelData.Width * 4);

                    return bitmap;
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al generar datos de píxeles para el código de barras");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error general al generar imagen de código de barras");
                return null;
            }
        }

        /// <summary>
        /// Guarda una imagen del código de barras en un archivo
        /// </summary>
        public string GuardarImagenCodigoBarras(string contenido, string rutaArchivo,
            BarcodeFormat formato = BarcodeFormat.CODE_128, int ancho = 300, int alto = 100)
        {
            try
            {
                var bitmap = GenerarImagenCodigoBarras(contenido, formato, ancho, alto);

                if (bitmap == null)
                {
                    _logService.Error("No se pudo generar la imagen del código de barras para guardar");
                    return null;
                }

                // Crear directorio si no existe
                string directorio = Path.GetDirectoryName(rutaArchivo);
                if (!Directory.Exists(directorio))
                {
                    Directory.CreateDirectory(directorio);
                }

                // Guardar imagen en formato PNG
                using (var fileStream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fileStream);
                }

                _logService.Debug($"Imagen de código de barras guardada en: {rutaArchivo}");
                return rutaArchivo;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al guardar imagen de código de barras");
                return null;
            }
        }
    }
}