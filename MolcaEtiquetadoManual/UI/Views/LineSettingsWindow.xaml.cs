using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using System.IO;
using System.Linq;

namespace MolcaEtiquetadoManual.UI.Views
{
    public partial class LineSettingsWindow : Window
    {
        private readonly ILogService _logService;
        private readonly ILineaProduccionService _lineaService;
        private readonly IConfiguration _configuration;
        private bool _settingsChanged = false;

        public LineSettingsWindow(ILogService logService, ILineaProduccionService lineaService, IConfiguration configuration)
        {
            InitializeComponent();

            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _lineaService = lineaService ?? throw new ArgumentNullException(nameof(lineaService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Cargar configuración actual
            CargarConfiguracion();
        }

        private void CargarConfiguracion()
        {
            try
            {
                // Obtener línea actual desde la configuración
                var lineNumber = _configuration.GetValue<string>("AppSettings:LineNumber") ?? "1";

                // Seleccionar la línea en el combo
                foreach (ComboBoxItem item in cmbNroLinea.Items)
                {
                    if (item.Content.ToString() == lineNumber)
                    {
                        cmbNroLinea.SelectedItem = item;
                        break;
                    }
                }

                // Si no hay selección, seleccionar la primera
                if (cmbNroLinea.SelectedItem == null && cmbNroLinea.Items.Count > 0)
                {
                    cmbNroLinea.SelectedIndex = 0;
                }

                // Obtener datos de la línea desde la BD
                int lineId = int.Parse(lineNumber);
                var linea = _lineaService.GetLineaById(lineId);

                if (linea != null)
                {
                    txtNombre.Text = linea.Nombre;
                    txtDescripcion.Text = linea.Descripcion;
                    chkActiva.IsChecked = linea.Activa;
                }
                else
                {
                    // Si no existe en BD, usar datos de la configuración
                    txtNombre.Text = _configuration.GetValue<string>("AppSettings:LineName") ?? $"Línea {lineNumber}";
                    txtDescripcion.Text = $"Línea de producción {lineNumber}";
                    chkActiva.IsChecked = true;
                }

                _logService.Information("Configuración de línea cargada en ventana de configuración");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al cargar configuración de línea");
                MessageBox.Show($"Error al cargar la configuración: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar datos básicos
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    txtError.Text = "Debe ingresar un nombre para la línea";
                    txtNombre.Focus();
                    return;
                }

                if (cmbNroLinea.SelectedItem == null)
                {
                    txtError.Text = "Debe seleccionar un número de línea";
                    cmbNroLinea.Focus();
                    return;
                }

                // Obtener valores
                string lineNumber = ((ComboBoxItem)cmbNroLinea.SelectedItem).Content.ToString();
                string nombre = txtNombre.Text.Trim();
                string descripcion = txtDescripcion.Text?.Trim() ?? "";
                bool activa = chkActiva.IsChecked ?? true;

                // Guardar en la base de datos
                int lineId = int.Parse(lineNumber);
                var linea = _lineaService.GetLineaById(lineId);

                if (linea == null)
                {
                    // Crear nueva línea
                    linea = new LineaProduccion
                    {
                        Id = lineId,
                        Nombre = nombre,
                        Descripcion = descripcion,
                        Activa = activa
                    };
                    _lineaService.AddLinea(linea);
                }
                else
                {
                    // Actualizar línea existente
                    linea.Nombre = nombre;
                    linea.Descripcion = descripcion;
                    linea.Activa = activa;
                    _lineaService.UpdateLinea(linea);
                }

                // Actualizar archivo appsettings.json
                ActualizarArchivoConfig(lineNumber, nombre);

                _logService.Information("Configuración de línea actualizada: Línea {LineNumber}, Nombre: {Nombre}", lineNumber, nombre);

                MessageBox.Show($"Configuración de línea guardada correctamente.\nLa aplicación usará la Línea {lineNumber}: {nombre}",
                    "Configuración guardada", MessageBoxButton.OK, MessageBoxImage.Information);

                _settingsChanged = true;
                Close();
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al guardar configuración de línea");
                MessageBox.Show($"Error al guardar la configuración: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarArchivoConfig(string lineNumber, string lineName)
        {
            try
            {
                // Ruta del archivo de configuración
                string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                // Leer el archivo
                string json = File.ReadAllText(appSettingsPath);

                // Crear configuración temporal para actualizar
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(appSettingsPath)
                    .Build();

                // Crear un editor para modificar el archivo
                var root = System.Text.Json.JsonDocument.Parse(json).RootElement;
                var jsonObject = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);

                // Obtener o crear la sección AppSettings
                if (!jsonObject.ContainsKey("AppSettings"))
                {
                    jsonObject["AppSettings"] = new System.Collections.Generic.Dictionary<string, object>();
                }

                var appSettings = jsonObject["AppSettings"] as System.Collections.Generic.Dictionary<string, object>;
                if (appSettings == null)
                {
                    // Crear un nuevo diccionario si el casting falló
                    appSettings = new System.Collections.Generic.Dictionary<string, object>();
                    jsonObject["AppSettings"] = appSettings;
                }

                // Ahora puedes asignar valores con seguridad
                appSettings["LineNumber"] = lineNumber;
                appSettings["LineName"] = lineName;
         

                // Guardar cambios
                string updatedJson = System.Text.Json.JsonSerializer.Serialize(jsonObject, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(appSettingsPath, updatedJson);

                _logService.Information("Archivo de configuración actualizado con la línea {LineNumber}", lineNumber);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al actualizar archivo de configuración");
                throw new Exception("No se pudo actualizar el archivo de configuración", ex);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CmbNroLinea_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbNroLinea.SelectedItem != null)
            {
                try
                {
                    // Obtener la línea seleccionada
                    string lineNumber = ((ComboBoxItem)cmbNroLinea.SelectedItem).Content.ToString();
                    int lineId = int.Parse(lineNumber);

                    // Buscar la línea en la BD
                    var linea = _lineaService.GetLineaById(lineId);

                    if (linea != null)
                    {
                        // Actualizar los campos con los datos de la línea
                        txtNombre.Text = linea.Nombre;
                        txtDescripcion.Text = linea.Descripcion;
                        chkActiva.IsChecked = linea.Activa;
                    }
                    else
                    {
                        // Línea no existe, mostrar valores por defecto
                        txtNombre.Text = $"Línea {lineNumber}";
                        txtDescripcion.Text = $"Línea de producción {lineNumber}";
                        chkActiva.IsChecked = true;
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al cargar datos de la línea seleccionada");
                }
            }
        }

        // Propiedad para saber si se guardaron cambios
        public bool SettingsChanged => _settingsChanged;
    }
}