using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.UI.Controls;
using Microsoft.Extensions.Configuration;

namespace MolcaEtiquetadoManual.UI.Views
{
    public partial class MainWindow : Window
    {
        private readonly IEtiquetadoService _etiquetadoService;
        private readonly IUsuarioService _usuarioService;
        private readonly IPrintService _printService;
        private readonly ITurnoService _turnoService;
        private readonly ILogService _logService;
        private readonly IBarcodeService _barcodeService;
        private readonly IJulianDateService _julianDateService;
        private readonly IConfiguration _configuration;
        private readonly IEtiquetaPreviewService _etiquetaPreviewService;

        private readonly Usuario _currentUser;

        // Controles de pasos
        private Step1Control _step1Control;
        private Step2Control _step2Control;
        private Step3Control _step3Control;

        // Colección observable para la lista de actividad
        public ObservableCollection<ActivityLogItem> ActivityItems { get; set; }

        public MainWindow(Usuario currentUser,
                        IEtiquetadoService etiquetadoService,
                        IUsuarioService usuarioService,
                        IPrintService printService,
                        ITurnoService turnoService,
                        ILogService logService,
                        IEtiquetaPreviewService etiquetaPreviewService,
                        IBarcodeService barcodeService = null,
                        IJulianDateService julianDateService = null,
                        IConfiguration configuration = null)
        {
            InitializeComponent();

            // Validación explícita para evitar NullReferenceException
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _etiquetadoService = etiquetadoService ?? throw new ArgumentNullException(nameof(etiquetadoService));
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _turnoService = turnoService ?? throw new ArgumentNullException(nameof(turnoService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _etiquetaPreviewService = etiquetaPreviewService ?? throw new ArgumentNullException(nameof(etiquetaPreviewService));

            // Estos pueden ser opcionales, así que no lanzamos excepciones
            _barcodeService = barcodeService;
            _julianDateService = julianDateService;
            _configuration = configuration;

            // Inicializar la colección de actividad
            ActivityItems = new ObservableCollection<ActivityLogItem>();
            lvActividad.ItemsSource = ActivityItems;

            // Establecer DataContext para binding
            DataContext = this;

            // Mostrar información del usuario
            txtUsuarioActual.Text = $"Usuario: {_currentUser.NombreUsuario} [{_currentUser.Rol}]";

            // Mostrar versión de la aplicación
            string appVersion = "1.0.0"; // Valor predeterminado
            string companyName = "Molca"; // Valor predeterminado

            // Solo intentar leer la configuración si no es nula
            if (_configuration != null)
            {
                try
                {
                    var appSettings = _configuration.GetSection("AppSettings");
                    if (appSettings != null)
                    {
                        appVersion = appSettings["Version"] ?? appVersion;
                        companyName = appSettings["Company"] ?? companyName;
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al leer la configuración de la aplicación");
                }
            }

            txtVersion.Text = $"{companyName} - Sistema de Etiquetado Manual v{appVersion}";

            // Inicializar y configurar los controles de pasos
            InitializeStepControls();

            // Configurar manejadores de eventos del wizard
            stepWizard.StepChanged += StepWizard_StepChanged;

            // Registrar inicio de sesión
            _logService.Information("Sesión iniciada - Usuario: {Username}, Rol: {Role}",
                _currentUser.NombreUsuario, _currentUser.Rol);

            AddActivityLogItem("Bienvenido al Sistema de Etiquetado Manual", ActivityLogItem.LogLevel.Info);
            AddActivityLogItem("Escanee un código DUN-14 para comenzar", ActivityLogItem.LogLevel.Info);
        }

        private void InitializeStepControls()
        {
            try
            {
                // Inicializar control de Paso 1
                _step1Control = new Step1Control(_etiquetadoService, _logService);
                _step1Control.OrdenEncontrada += Step1Control_OrdenEncontrada;
                _step1Control.ActivityLog += AddActivityLogItem;
                stepWizard.SetStep1Content(_step1Control);

                // Inicializar control de Paso 2
                _step2Control = new Step2Control(
                    _printService,
                    _etiquetadoService,
                    _turnoService,
                    _barcodeService,
                    _logService,
                    _julianDateService,
                    _etiquetaPreviewService,
                    _currentUser);
                _step2Control.EtiquetaImpresa += Step2Control_EtiquetaImpresa;
                _step2Control.CancelarSolicitado += Step2Control_CancelarSolicitado;
                _step2Control.ActivityLog += AddActivityLogItem;
                stepWizard.SetStep2Content(_step2Control);

                // Inicializar control de Paso 3
                _step3Control = new Step3Control(_etiquetadoService, _barcodeService, _logService);
                _step3Control.EtiquetaConfirmada += Step3Control_EtiquetaConfirmada;
                _step3Control.CancelarSolicitado += Step3Control_CancelarSolicitado;
                _step3Control.ActivityLog += AddActivityLogItem;
                stepWizard.SetStep3Content(_step3Control);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al inicializar controles de pasos");
                MessageBox.Show($"Error al inicializar la aplicación: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Manejadores de eventos de Step1Control
        private void Step1Control_OrdenEncontrada(object sender, OrdenProduccionEventArgs e)
        {
            // Pasar la orden al control del paso 2
            _step2Control.SetOrden(e.Orden);

            // Avanzar al paso 2
            stepWizard.NextStep();
        }
        #endregion

        #region Manejadores de eventos de Step2Control
        private void Step2Control_EtiquetaImpresa(object sender, EtiquetaGeneradaEventArgs e)
        {
            // Pasar la etiqueta al control del paso 3
            _step3Control.SetEtiqueta(e.Etiqueta, e.CodigoBarras);

            // Avanzar al paso 3
            stepWizard.NextStep();
        }

        private void Step2Control_CancelarSolicitado(object sender, EventArgs e)
        {
            // Volver al paso 1 y limpiar los datos actuales
            _step2Control.Limpiar();
            stepWizard.GoToStep(1);
        }
        #endregion

        #region Manejadores de eventos de Step3Control
        private void Step3Control_EtiquetaConfirmada(object sender, EventArgs e)
        {
            // Registrar confirmación
            AddActivityLogItem("Etiqueta confirmada correctamente. Listo para un nuevo ciclo.", ActivityLogItem.LogLevel.Info);

            // Resetear controles
            _step1Control.Reiniciar();
            _step2Control.Limpiar();
            _step3Control.Limpiar();

            // Volver al paso 1 para un nuevo ciclo
            stepWizard.GoToStep(1);
        }

        private void Step3Control_CancelarSolicitado(object sender, EventArgs e)
        {
            // Volver al paso 2
            stepWizard.GoToStep(2);
        }
        #endregion

        #region Manejadores de eventos del StepWizard
        private void StepWizard_StepChanged(object sender, StepChangedEventArgs e)
        {
            // Actualizar el estado según el paso actual
            switch (e.NewStep)
            {
                case 1:
                    txtEstado.Text = "Paso 1: Escanee un código DUN14 para comenzar";
                    break;
                case 2:
                    txtEstado.Text = "Paso 2: Revise los datos y genere la etiqueta";
                    break;
                case 3:
                    txtEstado.Text = "Paso 3: Verifique y confirme la etiqueta impresa";
                    break;
            }

            // Poner foco adecuado según el paso
            if (e.NewStep == 1)
                _step1Control.Reiniciar();
        }
        #endregion

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            _logService.Information("Sesión cerrada - Usuario: {Username}", _currentUser.NombreUsuario);

            var loginWindow = new LoginWindow(_usuarioService, _etiquetadoService, _printService,
                                            _turnoService, _logService, _etiquetaPreviewService, _barcodeService,
                                            _julianDateService, _configuration);
            loginWindow.Show();
            this.Close();
        }

        //private void BtnConfigImpresora_Click(object sender, RoutedEventArgs e)
        //{
        //    // Versión simplificada solo para pruebas
        //    MessageBox.Show("Configuración de impresora no implementada en esta versión mínima",
        //        "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        //}

        //private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        //{
        //    // Versión simplificada solo para pruebas
        //    MessageBox.Show("Gestión de usuarios no implementada en esta versión mínima",
        //        "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        //}
        private void BtnConfigImpresora_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verificar si el usuario tiene permisos (solo administradores)
                if (_currentUser.Rol != "Administrador")
                {
                    MessageBox.Show("Solo los administradores pueden acceder a la configuración de impresora.",
                        "Acceso Restringido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Crear y mostrar la ventana de configuración
                var configWindow = new PrinterSettingsWindow(_logService, _configuration);
                configWindow.Owner = this;

                _logService.Information("Abriendo ventana de configuración de impresora - Usuario: {Username}",
                    _currentUser.NombreUsuario);

                // Mostrar como diálogo modal
                configWindow.ShowDialog();

                // Registrar en el historial
                if (configWindow.SettingsChanged)
                {
                    AddActivityLogItem("Configuración de impresora actualizada", ActivityLogItem.LogLevel.Info);
                }
                else
                {
                    AddActivityLogItem("Se accedió a la configuración de impresora", ActivityLogItem.LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al abrir ventana de configuración de impresora");
                MessageBox.Show($"Error al abrir la configuración: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verificar permisos (solo administradores)
                if (_currentUser.Rol != "Administrador")
                {
                    MessageBox.Show("Solo los administradores pueden acceder a la gestión de usuarios.",
                        "Acceso Restringido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Crear y mostrar la ventana de gestión de usuarios
                var userManagementWindow = new UserManagementWindow(_currentUser, _usuarioService, _logService);
                userManagementWindow.Owner = this;

                _logService.Information("Abriendo ventana de gestión de usuarios - Usuario: {Username}",
                    _currentUser.NombreUsuario);

                // Mostrar como diálogo modal
                userManagementWindow.ShowDialog();

                // Registrar en el historial
                AddActivityLogItem("Se accedió a la gestión de usuarios", ActivityLogItem.LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al abrir ventana de gestión de usuarios");
                MessageBox.Show($"Error al abrir la gestión de usuarios: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddActivityLogItem(string message, ActivityLogItem.LogLevel level = ActivityLogItem.LogLevel.Info)
        {
            // Crear un nuevo ítem de log
            var logItem = new ActivityLogItem
            {
                Description = message,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Level = level
            };

            // Agregar al inicio para que los más recientes estén arriba
            Application.Current.Dispatcher.Invoke(() =>
            {
                ActivityItems.Insert(0, logItem);
            });

            // También actualizamos el estado para mostrar el mensaje más reciente
            if (level == ActivityLogItem.LogLevel.Info)
                txtEstado.Text = message;

            // Registrar en el log del sistema según el nivel
            switch (level)
            {
                case ActivityLogItem.LogLevel.Info:
                    _logService.Information(message);
                    break;
                case ActivityLogItem.LogLevel.Warning:
                    _logService.Warning(message);
                    break;
                case ActivityLogItem.LogLevel.Error:
                    _logService.Error(message);
                    break;
                case ActivityLogItem.LogLevel.Debug:
                    _logService.Debug(message);
                    break;
            }
        }
    }
}