using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;


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
                txtLabelQuantity.Text = printerSettings["LabelQuantity"] ?? "1";
                chkUseMockPrinter.IsChecked = bool.Parse(printerSettings["UseMockPrinter"] ?? "true");
                chkShowPrintDialog.IsChecked = bool.Parse(printerSettings["ShowPrintDialog"] ?? "true");

                // Configurar las rutas de logs
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                // Obtener las rutas desde el archivo de configuración o usar valores predeterminados
                var logsSection = _configuration.GetSection("Serilog:WriteTo");
                string logDirectory = appDataPath;

                if (logsSection != null && logsSection.GetChildren().Any())
                {
                    foreach (var sink in logsSection.GetChildren())
                    {
                        if (sink["Name"]?.Equals("File", StringComparison.OrdinalIgnoreCase) == true &&
                            sink["Args"] != null && sink["Args:path"] != null)
                        {
                            // Extraer la ruta del archivo de log
                            string logPath = sink["Args:path"];

                            // Reemplazar variables de entorno si existen
                            if (logPath.Contains("%APPDATA%"))
                            {
                                logPath = logPath.Replace("%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                            }
                            else if (logPath.Contains("%LOCALAPPDATA%"))
                            {
                                logPath = logPath.Replace("%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                            }

                            // Obtener el directorio del archivo de log
                            logDirectory = Path.GetDirectoryName(logPath);
                            break;
                        }
                    }
                }

                // Si no se encontró una configuración válida, usar la predeterminada
                if (string.IsNullOrEmpty(logDirectory))
                {
                    logDirectory = Path.Combine(appDataPath, "MolcaEtiquetadoManual", "Logs");
                }

                txtLogDirectory.Text = logDirectory;
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

                // Validar cantidad de etiquetas
                if (!int.TryParse(txtLabelQuantity.Text, out int labelQuantity) || labelQuantity <= 0)
                {
                    MessageBox.Show("La cantidad de etiquetas debe ser un número mayor a 0",
                        "Error de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtLabelQuantity.Focus();
                    return;
                }

                // Guardar los valores en el archivo de configuración
                GuardarConfiguracion();

                _logService.Information("Configuración de impresora actualizada");
                MessageBox.Show("Configuración guardada correctamente",
                    "Configuración guardada", MessageBoxButton.OK, MessageBoxImage.Information);
                MessageBox.Show("La aplicación necesita reiniciarse para aplicar los cambios. Por favor, cierre y vuelva a abrir la aplicación.",
                    "Reinicio requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void GuardarConfiguracion()
        {
            try
            {
                // Ruta al archivo de configuración
                string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                // Leer todas las configuraciones existentes
                var options = new JsonSerializerOptions { WriteIndented = true };
                Dictionary<string, object> jsonConfig;

                if (File.Exists(appSettingsPath))
                {
                    string jsonContent = File.ReadAllText(appSettingsPath);
                    jsonConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                }
                else
                {
                    jsonConfig = new Dictionary<string, object>();
                }

                // Crear o actualizar la sección PrinterSettings
                var printerSettings = new Dictionary<string, object>
                {
                    ["IpAddress"] = txtIpAddress.Text,
                    ["Port"] = int.Parse(txtPort.Text),
                    ["FormatName"] = txtFormatName.Text,
                    ["FormatUnit"] = txtFormatUnit.Text,
                    ["LabelQuantity"] = int.Parse(txtLabelQuantity.Text),
                    ["UseMockPrinter"] = chkUseMockPrinter.IsChecked == true,
                    ["ShowPrintDialog"] = chkShowPrintDialog.IsChecked == true
                };

                // Actualizar en el objeto principal
                jsonConfig["PrinterSettings"] = printerSettings;

                // Guardar todo el archivo
                string updatedJson = JsonSerializer.Serialize(jsonConfig, options);
                File.WriteAllText(appSettingsPath, updatedJson);

                _logService.Information("Archivo de configuración actualizado correctamente");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al guardar archivo de configuración");
                throw new Exception("No se pudo actualizar el archivo de configuración", ex);
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
                int portNumber = int.Parse(txtPort.Text);

                // Mostrar indicador de progreso
                var originalContent = btnTestPrinter.Content;
                btnTestPrinter.IsEnabled = false;
                btnTestPrinter.Content = "Probando...";

                // Crear una tarea para probar la conexión en segundo plano
                Task.Run(() =>
                {
                    bool success = false;
                    string message = "Conexión exitosa con la impresora.";

                    try
                    {
                        // Intentar establecer una conexión TCP a la impresora
                        using (var client = new System.Net.Sockets.TcpClient())
                        {
                            var task = client.ConnectAsync(ipAddress, portNumber);
                            // Esperar hasta 3 segundos para la conexión
                            if (task.Wait(3000))
                            {
                                success = true;
                            }
                            else
                            {
                                message = "Tiempo de espera agotado al intentar conectar con la impresora.";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        message = $"Error al conectar con la impresora: {ex.Message}";
                        _logService.Error(ex, "Error al probar conexión con impresora {IP}:{Port}", ipAddress, portNumber);
                    }

                    // Volver al hilo de la UI para mostrar el resultado
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Restaurar el botón
                        btnTestPrinter.IsEnabled = true;
                        btnTestPrinter.Content = originalContent;

                        // Mostrar el resultado
                        MessageBox.Show(message,
                            success ? "Prueba exitosa" : "Error de conexión",
                            MessageBoxButton.OK,
                            success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                        _logService.Information("Prueba de conexión: {Result}", success ? "Exitosa" : "Fallida");
                    });
                });
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error en la prueba de conexión");
                MessageBox.Show($"Error al iniciar la prueba: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnTestPrinter.IsEnabled = true;
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

        // Método para validar entrada - solo números
        private void TxtLabelQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permitir dígitos
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        // Propiedad para saber si se guardaron cambios
        public bool SettingsChanged => _settingsChanged;
    }
}