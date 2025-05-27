using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.UI.Controls;
using Microsoft.Extensions.Configuration;
using MolcaEtiquetadoManual.Core.Services;
using System.Windows.Input;

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
        private readonly ILineaProduccionService _lineaService;
        private readonly Usuario _currentUser;
        private bool enciclo = false;
        // Controles de pasos
        private Step1Control _step1Control;
        private Step2Control _step2Control;
        private Step3Control _step3Control;
        private readonly KioskManager _kioskManager;
        private readonly bool _kioskModeEnabled;

        // Colección observable para la lista de actividad
        public ObservableCollection<ActivityLogItem> ActivityItems { get; set; }

        public MainWindow(Usuario currentUser,
                        IEtiquetadoService etiquetadoService,
                        IUsuarioService usuarioService,
                        IPrintService printService,
                        ITurnoService turnoService,
                        ILogService logService,
                        IEtiquetaPreviewService etiquetaPreviewService,
                        ILineaProduccionService lineaService,
                        IBarcodeService barcodeService = null,
                        IJulianDateService julianDateService = null,
                        IConfiguration configuration = null,
                        KioskManager kioskManager = null)
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            // Validación explícita para evitar NullReferenceException
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _etiquetadoService = etiquetadoService ?? throw new ArgumentNullException(nameof(etiquetadoService));
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _turnoService = turnoService ?? throw new ArgumentNullException(nameof(turnoService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _etiquetaPreviewService = etiquetaPreviewService ?? throw new ArgumentNullException(nameof(etiquetaPreviewService));
            _lineaService = lineaService ?? throw new ArgumentNullException(nameof(lineaService));

            // Estos pueden ser opcionales, así que no lanzamos excepciones
            _barcodeService = barcodeService;
            _julianDateService = julianDateService;
            _configuration = configuration;
            // Configuración de Kiosk
            _kioskManager = kioskManager;
            _kioskModeEnabled = _configuration?.GetSection("KioskSettings").GetValue<bool>("Enabled", false) ?? false;

            // Si el modo Kiosk está habilitado, configurar la ventana
            if (_kioskModeEnabled && _kioskManager != null)
            {
                this.Loaded += MainWindow_Loaded;
                ConfigureKioskUI();
            }

            ActualizarTituloConNumeroLinea();
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
                        
            if (_kioskModeEnabled)
            {
                AddActivityLogItem("MODO KIOSK ACTIVADO - Presione Ctrl+Shift+Alt+F12 para salida de emergencia", ActivityLogItem.LogLevel.Warning);
            }

            AddActivityLogItem("Escanee un código DUN-14 para comenzar", ActivityLogItem.LogLevel.Info);
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_kioskModeEnabled && _kioskManager != null)
            {
                try
                {
                    _kioskManager.EnableKioskMode(this);
                    _logService.Information("Modo Kiosk activado en ventana principal");
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al activar modo Kiosk en ventana principal");
                    AddActivityLogItem("Error al activar modo Kiosk", ActivityLogItem.LogLevel.Error);
                }
            }
        }

        private void ConfigureKioskUI()
        {
            try
            {
                if (_kioskModeEnabled)
                {
                    // En modo Kiosk, agregar indicador visual
                    this.Title += " - MODO KIOSK";
                    _logService.Information("Interfaz configurada para modo Kiosk");
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al configurar UI para modo Kiosk");
            }
        }

        // Manejar teclas especiales en modo Kiosk
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_kioskModeEnabled)
            {
                // F1 para mostrar ayuda rápida en modo Kiosk
                if (e.Key == Key.F1)
                {
                    e.Handled = true;
                    ShowKioskHelp();
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        private void ShowKioskHelp()
        {
            var helpMessage = "SISTEMA DE ETIQUETADO MANUAL - MODO KIOSK\n\n" +
                             "• Esta aplicación está ejecutándose en modo restringido\n" +
                             "• Solo las funciones de etiquetado están disponibles\n" +
                             "• Para salir: Ctrl+Shift+Alt+F12 (solo emergencias)\n" +
                             "• Para cambio de turno: usar 'Cerrar Sesión'\n\n" +
                             "Versión: " + (_configuration?.GetSection("AppSettings")["Version"] ?? "1.0.0");

            MessageBox.Show(helpMessage, "Ayuda - Modo Kiosk",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (enciclo)
            {
                // Hay un proceso en curso, no permitir cerrar
                e.Cancel = true;
                _logService.Warning("No se puede cerrar la aplicación mientras hay un proceso de impresión en curso.", _currentUser.NombreUsuario);
                AddActivityLogItem("No se puede cerrar la aplicación mientras hay un proceso de impresión en curso.", ActivityLogItem.LogLevel.Warning);

                MessageBox.Show(
                    "No se puede cerrar la aplicación mientras hay un proceso de impresión en curso.",
                    "Operación en proceso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            // AGREGAR ESTA SECCIÓN:
            else if (_kioskModeEnabled)
            {
                // En modo Kiosk, no permitir cerrar fácilmente
                e.Cancel = true;

                var result = MessageBox.Show(
                    "¿Está seguro que desea cerrar la aplicación?\n\nEsto desactivará el modo Kiosk.",
                    "Confirmar cierre - Modo Kiosk",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _logService.Warning("Usuario confirmó cierre de aplicación en modo Kiosk");

                    // Deshabilitar Kiosk y permitir cierre
                    if (_kioskManager != null)
                    {
                        _kioskManager.DisableKioskMode();
                    }

                    e.Cancel = false;
                    Application.Current.Shutdown();
                }
            }
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
                    _currentUser,
                    _configuration,
                    _lineaService);
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
            enciclo = true;
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
            enciclo=false;
            // Volver al paso 1 y limpiar los datos actuales
            _step2Control.Limpiar();
            stepWizard.GoToStep(1);
        }
        #endregion

        #region Manejadores de eventos de Step3Control
        private void Step3Control_EtiquetaConfirmada(object sender, EventArgs e)
        {
            ActivityItems.Clear();
            // Registrar confirmación
            AddActivityLogItem("Etiqueta confirmada correctamente. Listo para un nuevo ciclo.", ActivityLogItem.LogLevel.Info);

            // Resetear controles
            _step1Control.Reiniciar();
            _step2Control.Limpiar();
            _step3Control.Limpiar();

            // Volver al paso 1 para un nuevo ciclo
            stepWizard.GoToStep(1);
            enciclo = false;
        }

        private void Step3Control_CancelarSolicitado(object sender, EventArgs e)
        {
            // Obtener la orden actual del Step2
            ActivityItems.Clear();
            // Registrar confirmación
            AddActivityLogItem("Etiqueta cancelada. Listo para un nuevo ciclo.", ActivityLogItem.LogLevel.Warning);

            // Resetear controles
            _step1Control.Reiniciar();
            _step2Control.Limpiar();
            _step3Control.Limpiar();

            // Volver al paso 1 para un nuevo ciclo
            stepWizard.GoToStep(1);
            enciclo = false;
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
            if (enciclo)
            {
                _logService.Warning("No se puede cerrar sesión mientras hay un proceso de impresión en curso.", _currentUser.NombreUsuario);
                AddActivityLogItem("No se puede cerrar sesión mientras hay un proceso de impresión en curso.", ActivityLogItem.LogLevel.Warning);
                MessageBox.Show("No se puede cerrar sesión mientras hay un proceso de impresión en curso.",
                       "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                // MODIFICAR ESTA SECCIÓN:
                // En modo Kiosk, solo cerrar sesión pero mantener la aplicación abierta
                if (_kioskModeEnabled)
                {
                    var result = MessageBox.Show(
                        "¿Desea cerrar la sesión actual?\n\nLa aplicación permanecerá abierta para el siguiente usuario.",
                        "Cerrar Sesión - Modo Kiosk",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _logService.Information("Sesión cerrada en modo Kiosk - Usuario: {Username}", _currentUser.NombreUsuario);

                        // Crear nueva ventana de login con Kiosk habilitado
                        var loginWindow = new LoginWindow(_usuarioService, _etiquetadoService, _printService,
                                                        _turnoService, _logService, _etiquetaPreviewService,
                                                        _lineaService, _barcodeService, _julianDateService,
                                                        _configuration, _kioskManager);  // ← Pasar KioskManager

                        // Configurar la nueva ventana de login para Kiosk
                        if (_kioskManager != null)
                        {
                            loginWindow.Loaded += (s, ev) => _kioskManager.EnableKioskMode(loginWindow);
                        }

                        loginWindow.Show();

                        // Cerrar ventana actual sin deshabilitar Kiosk
                        this.Loaded -= MainWindow_Loaded; // Desuscribirse del evento
                        this.Close();
                    }
                }
                else
                {
                    // Comportamiento normal (no Kiosk)
                    _logService.Information("Sesión cerrada - Usuario: {Username}", _currentUser.NombreUsuario);

                    var loginWindow = new LoginWindow(_usuarioService, _etiquetadoService, _printService,
                                                    _turnoService, _logService, _etiquetaPreviewService,
                                                    _lineaService, _barcodeService, _julianDateService, _configuration);
                    loginWindow.Show();
                    this.Close();
                }
            }
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

        private void BtnConfigLinea_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verificar si el usuario tiene permisos (solo administradores)
                if (_currentUser.Rol != "Administrador")
                {
                    MessageBox.Show("Solo los administradores pueden acceder a la configuración de línea.",
                        "Acceso Restringido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Crear y mostrar la ventana de configuración

                var configWindow = new LineSettingsWindow(_logService, _lineaService, _configuration);
                configWindow.Owner = this;

                _logService.Information("Abriendo ventana de configuración de línea - Usuario: {Username}",
                    _currentUser.NombreUsuario);

                // Mostrar como diálogo modal
                configWindow.ShowDialog();

                // Si se cambiaron las configuraciones, actualizar el título con el nuevo número de línea
                if (configWindow.SettingsChanged)
                {
                    ActualizarTituloConNumeroLinea();
                    AddActivityLogItem("Configuración de línea actualizada", ActivityLogItem.LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al abrir ventana de configuración de línea");
                MessageBox.Show($"Error al abrir la configuración de línea: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para actualizar el título de la ventana con el número de línea
        private void ActualizarTituloConNumeroLinea()
        {
            try
            {
                var lineNumber = _configuration.GetValue<string>("AppSettings:LineNumber") ?? "1";
                var lineName = _configuration.GetValue<string>("AppSettings:LineName") ?? $"Línea {lineNumber}";

                // Actualizar el título de la ventana
                this.Title = $"Sistema de Etiquetado Manual - {lineName}";
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al actualizar título con número de línea");
            }
        }
    }
}