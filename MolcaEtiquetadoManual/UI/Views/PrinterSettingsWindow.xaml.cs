using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using MolcaEtiquetadoManual.Core.Interfaces;

namespace MolcaEtiquetadoManual.UI.Views
{
    public partial class PrinterSettingsWindow : Window
    {
        private readonly ILogService _logService;
        private readonly IConfiguration _configuration;
        private bool _settingsChanged = false;

        public PrinterSettingsWindow(ILogService logService, IConfiguration configuration)
        {
            InitializeComponent();

            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Cargar configuración actual
            CargarConfiguracion();
        }

        private void CargarConfiguracion()
        {
            try
            {
                var printerSettings = _configuration.GetSection("PrinterSettings");

                txtIpAddress.Text = printerSettings["IpAddress"] ?? "192.168.1.100";
                txtPort.Text = printerSettings["Port"] ?? "9100";
                txtFormatName.Text = printerSettings["FormatName"] ?? "MOLCA.ZPL";
                txtFormatUnit.Text = printerSettings["FormatUnit"] ?? "E";
                chkUseMockPrinter.IsChecked = bool.Parse(printerSettings["UseMockPrinter"] ?? "true");
                chkShowPrintDialog.IsChecked = bool.Parse(printerSettings["ShowPrintDialog"] ?? "true");

                // Información sobre directorios de logs
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                txtLogDirectory.Text = Path.Combine(appDataPath, "MolcaEtiquetadoManual", "Logs");
                txtZplDebugDirectory.Text = Path.Combine(appDataPath, "MolcaEtiquetadoManual", "ZplDebug");

                _logService.Information("Configuración de impresora cargada en ventana de configuración");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al cargar configuración de impresora");
                MessageBox.Show($"Error al cargar la configuración: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar datos básicos
                if (string.IsNullOrWhiteSpace(txtIpAddress.Text))
                {
                    MessageBox.Show("Debe ingresar la dirección IP de la impresora",
                        "Error de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtIpAddress.Focus();
                    return;
                }

                if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("El puerto debe ser un número entre 1 y 65535",
                        "Error de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPort.Focus();
                    return;
                }

                // En una aplicación real, aquí actualizaríamos el archivo appsettings.json
                // Para esta versión mínima, solo mostramos los valores que se guardarían
                string mensaje = $"Configuración que se guardaría:\n\n" +
                    $"Dirección IP: {txtIpAddress.Text}\n" +
                    $"Puerto: {txtPort.Text}\n" +
                    $"Nombre de formato: {txtFormatName.Text}\n" +
                    $"Unidad de formato: {txtFormatUnit.Text}\n" +
                    $"Usar impresora simulada: {chkUseMockPrinter.IsChecked}\n" +
                    $"Mostrar diálogo de impresión: {chkShowPrintDialog.IsChecked}";

                _logService.Information("Configuración de impresora actualizada (simulación)");
                MessageBox.Show(mensaje, "Configuración guardada (simulación)",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _settingsChanged = true;
                Close();
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al guardar configuración de impresora");
                MessageBox.Show($"Error al guardar la configuración: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnTestPrinter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar datos básicos antes de probar
                if (string.IsNullOrWhiteSpace(txtIpAddress.Text))
                {
                    MessageBox.Show("Debe ingresar la dirección IP de la impresora",
                        "Error de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtIpAddress.Focus();
                    return;
                }

                if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("El puerto debe ser un número entre 1 y 65535",
                        "Error de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPort.Focus();
                    return;
                }

                string ipAddress = txtIpAddress.Text;

                // Simulación de prueba
                _logService.Information($"Probando conexión con impresora en {ipAddress}:{port}");

                // Mostrar un mensaje de prueba simulada
                MessageBox.Show($"Simulando prueba de conexión con la impresora en {ipAddress}:{port}\n\n" +
                    "En un entorno de producción, esto enviaría un comando ZPL de prueba.",
                    "Prueba de conexión", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al probar conexión con impresora");
                MessageBox.Show($"Error al probar la conexión: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logDirectory = txtLogDirectory.Text;

                if (Directory.Exists(logDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logDirectory);
                    _logService.Information($"Abriendo directorio de logs: {logDirectory}");
                }
                else
                {
                    MessageBox.Show($"El directorio de logs no existe: {logDirectory}",
                        "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al abrir directorio de logs");
                MessageBox.Show($"Error al abrir directorio de logs: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenZplDebug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string zplDebugDirectory = txtZplDebugDirectory.Text;

                if (Directory.Exists(zplDebugDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", zplDebugDirectory);
                    _logService.Information($"Abriendo directorio de debug ZPL: {zplDebugDirectory}");
                }
                else
                {
                    MessageBox.Show($"El directorio de debug ZPL no existe: {zplDebugDirectory}",
                        "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al abrir directorio de debug ZPL");
                MessageBox.Show($"Error al abrir directorio de debug ZPL: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Propiedad para saber si se guardaron cambios
        public bool SettingsChanged => _settingsChanged;
    }
}