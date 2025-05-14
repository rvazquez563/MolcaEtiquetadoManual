// Core/Services/TestPrintService.cs
using System;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Extensions.Configuration;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Services
{
    /// <summary>
    /// Implementación de IPrintService para entornos de prueba.
    /// Muestra una ventana de diálogo y guarda los comandos ZPL en archivos locales.
    /// </summary>
    public class TestPrintService : IPrintService
    {
        private readonly ILogService _logService;
        private readonly IConfiguration _configuration;
        private readonly int _labelQuantity;

        public TestPrintService(ILogService logService, IConfiguration configuration = null)
        {
            _logService = logService;
            _configuration = configuration;

            // Obtener la cantidad de etiquetas de la configuración
            _labelQuantity = 1; // Valor por defecto

            if (_configuration != null)
            {
                var printerSection = _configuration.GetSection("PrinterSettings");
                if (printerSection != null && printerSection["LabelQuantity"] != null)
                {
                    if (int.TryParse(printerSection["LabelQuantity"], out int quantity) && quantity > 0)
                    {
                        _labelQuantity = quantity;
                    }
                }
            }

            _logService.Information($"Inicializando servicio de impresión para PRUEBAS (Cantidad de etiquetas: {_labelQuantity})");
        }

        public bool ImprimirEtiqueta(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras)
        {
            // En el modo de prueba, delegamos al método de simulación
            return SimularImpresion(orden, etiqueta, codigoBarras);
        }

        public bool SimularImpresion(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras)
        {
            try
            {
                _logService.Information($"SIMULANDO impresión de {_labelQuantity} etiqueta(s)");

                // Crear un "ticket" visual de la etiqueta para mostrar
                StringBuilder ticketInfo = new StringBuilder();
                ticketInfo.AppendLine("=== SIMULACIÓN DE IMPRESIÓN DE ETIQUETA ===");
                ticketInfo.AppendLine($"Fecha: {DateTime.Now}");
                ticketInfo.AppendLine($"Artículo: {orden.NumeroArticulo} - {orden.Descripcion}");
                ticketInfo.AppendLine($"Cantidad: {orden.CantidadPorPallet} unidades por pallet");
                ticketInfo.AppendLine($"Programa: {orden.ProgramaProduccion}");
                ticketInfo.AppendLine($"Lote: {etiqueta.LOTN}");
                ticketInfo.AppendLine($"Vencimiento: {etiqueta.EXPR:dd/MM/yyyy}");
                ticketInfo.AppendLine($"Cantidad de etiquetas a imprimir: {_labelQuantity}");
                ticketInfo.AppendLine("------------------------------------------");
                ticketInfo.AppendLine($"CÓDIGO DE BARRAS: {codigoBarras}");
                ticketInfo.AppendLine("===========================================");

                // Guardar en archivo de log
                GuardarSimulacionEnArchivo(ticketInfo.ToString(), codigoBarras);

                // Mostrar diálogo
                MessageBox.Show(
                    ticketInfo.ToString(),
                    "Simulación de Impresión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error en simulación de impresión");
                MessageBox.Show(
                    $"Error en simulación: {ex.Message}",
                    "Error de Impresión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private void GuardarSimulacionEnArchivo(string ticketInfo, string codigoBarras)
        {
            try
            {
                // Crear directorio si no existe
                string directorio = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MolcaEtiquetadoManual", "EtiquetasSimuladas");

                if (!Directory.Exists(directorio))
                {
                    Directory.CreateDirectory(directorio);
                }

                // Guardar la simulación con timestamp
                string archivo = Path.Combine(directorio,
                    $"etiqueta_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                File.WriteAllText(archivo, ticketInfo);

                // También guardar el código de barras en un archivo separado para pruebas
                string archivoBarcode = Path.Combine(directorio,
                    $"barcode_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                File.WriteAllText(archivoBarcode, codigoBarras);

                _logService.Debug($"Simulación de etiqueta guardada en {archivo}");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al guardar simulación de etiqueta");
            }
        }

        public void CancelarImpresion()
        {
            _logService.Information("Simulación: Cancelando impresión...");
            // No hay nada que cancelar en la simulación, solo registramos el intento
        }
    }
}