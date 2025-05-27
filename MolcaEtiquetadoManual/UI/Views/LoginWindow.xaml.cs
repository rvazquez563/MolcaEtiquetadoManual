// UI/Views/LoginWindow.xaml.cs
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Core.Services;

namespace MolcaEtiquetadoManual.UI.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IEtiquetadoService _etiquetadoService;
        private readonly IPrintService _printService;
        private readonly ITurnoService _turnoService;
        private readonly ILogService _logService;
        private readonly IBarcodeService _barcodeService;
        private readonly IJulianDateService _julianDateService;
        private readonly IConfiguration _configuration;
        private readonly IEtiquetaPreviewService _etiquetaPreviewService;
        private readonly ILineaProduccionService _lineaService;
        private readonly KioskManager _kioskManager;
        private readonly bool _kioskModeEnabled;
        public LoginWindow(IUsuarioService usuarioService,
                         IEtiquetadoService etiquetadoService,
                         IPrintService printService,
                         ITurnoService turnoService,
                         ILogService logService,
                         IEtiquetaPreviewService etiquetaPreviewService ,
                          ILineaProduccionService lineaService,
                         IBarcodeService barcodeService = null,
                         IJulianDateService julianDateService = null,
                         IConfiguration configuration = null,
                        KioskManager kioskManager = null)
        {
            InitializeComponent();

            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _etiquetadoService = etiquetadoService ?? throw new ArgumentNullException(nameof(etiquetadoService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _turnoService = turnoService ?? throw new ArgumentNullException(nameof(turnoService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _etiquetaPreviewService = etiquetaPreviewService ?? throw new ArgumentNullException(nameof(etiquetaPreviewService));
            _lineaService = lineaService ?? throw new ArgumentNullException(nameof(lineaService));
            _barcodeService = barcodeService;
            _julianDateService = julianDateService;
            _configuration = configuration;
            // Configuración de Kiosk
            _kioskManager = kioskManager;
            _kioskModeEnabled = _configuration?.GetSection("KioskSettings").GetValue<bool>("Enabled", false) ?? false;

            // Cargar versión desde configuración
            if (_configuration != null)
            {
                string version = _configuration.GetSection("AppSettings")["Version"] ?? "1.0.0";
                txtVersion.Text = $"v{version}";

                // AGREGAR ESTAS LÍNEAS:
                if (_kioskModeEnabled)
                {
                    txtVersion.Text += " - KIOSK";
                }
            }

            _logService.Information("Ventana de login iniciada - Modo Kiosk: {KioskMode}", _kioskModeEnabled);

            // Enfocar el campo de usuario
            Loaded += (s, e) => txtUsername.Focus();

            // AGREGAR ESTAS LÍNEAS:
            // Si estamos en modo Kiosk, configurar la ventana
            if (_kioskModeEnabled)
            {
                ConfigureKioskLogin();
            }
            // Cargar versión desde configuración
            if (_configuration != null)
            {
                string version = _configuration.GetSection("AppSettings")["Version"] ?? "1.0.0";
                txtVersion.Text = $"v{version}";
            }

            _logService.Information("Ventana de login iniciada");

            // Enfocar el campo de usuario
            Loaded += LoginWindow_Loaded_Event;
        }
        private void LoginWindow_Loaded_Event(object sender, RoutedEventArgs e)
          {
        // Enfocar campo de usuario
        txtUsername.Focus();
        
        // DEBUG: Mostrar información de estado
        MessageBox.Show($"DEBUG INFO:\n" +
                       $"Kiosk Enabled: {_kioskModeEnabled}\n" +
                       $"KioskManager is null: {_kioskManager == null}\n" +
                       $"Window Title: {this.Title}", 
                       "DEBUG Kiosk", MessageBoxButton.OK);
        
        // Si modo Kiosk está habilitado, activarlo
        if (_kioskModeEnabled && _kioskManager != null)
        {
            try
            {
                _logService.Information("=== INICIANDO ACTIVACIÓN DE KIOSK ===");
                MessageBox.Show("Activando modo Kiosk ahora...", "DEBUG", MessageBoxButton.OK);
                
                _kioskManager.EnableKioskMode(this);
                
                MessageBox.Show("Kiosk activado - ¿Ves pantalla completa?", "DEBUG", MessageBoxButton.OK);
                _logService.Information("=== KIOSK ACTIVADO EXITOSAMENTE ===");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "ERROR al activar modo Kiosk");
                MessageBox.Show($"ERROR Kiosk: {ex.Message}\n\nStack: {ex.StackTrace}", 
                               "ERROR DEBUG", MessageBoxButton.OK);
            }
        }
        else
        {
            string reason = "";
            if (!_kioskModeEnabled) reason += "Kiosk no habilitado en config. ";
            if (_kioskManager == null) reason += "KioskManager es null. ";
            
            MessageBox.Show($"NO SE ACTIVÓ KIOSK: {reason}", "DEBUG", MessageBoxButton.OK);
        }
    }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_kioskModeEnabled)
            {
                // En modo Kiosk, no permitir cerrar la ventana de login fácilmente
                // a menos que se esté haciendo una transición normal
                var result = MessageBox.Show(
                    "¿Está seguro que desea cerrar la aplicación?\n\nEsto desactivará el modo Kiosk.",
                    "Confirmar cierre - Modo Kiosk",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    // Usuario confirmó el cierre, deshabilitar Kiosk
                    if (_kioskManager != null)
                    {
                        _kioskManager.DisableKioskMode();
                        _logService.Warning("Modo Kiosk deshabilitado por cierre de ventana de login");
                    }
                }
            }

            base.OnClosing(e);
        }
        private void ConfigureKioskLogin()
        {
            try
            {
                // Cambiar el título para indicar modo Kiosk
                this.Title += " - MODO KIOSK";

                _logService.Information("Ventana de login configurada para modo Kiosk");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al configurar login para modo Kiosk");
            }
        }
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login();
            }
        }

        private async void Login()
        {
            // Mostrar indicador de carga
            loginProgressBar.Visibility = Visibility.Visible;
            btnLogin.IsEnabled = false;
            txtError.Text = string.Empty;

            string nombreUsuario = txtUsername.Text;
            string contraseña = txtPassword.Password;

            if (string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(contraseña))
            {
                _logService.Warning("Intento de login con campos vacíos");
                txtError.Text = "Por favor, ingrese nombre de usuario y contraseña";
                ResetLoginControls();
                return;
            }

            try
            {
                _logService.Information("Intento de autenticación: usuario {Username}", nombreUsuario);

                // Usar Task.Run para simular proceso asíncrono y mostrar el progreso
                var usuario = await Task.Run(() => _usuarioService.Authenticate(nombreUsuario, contraseña));

                if (usuario != null)
                {
                    _logService.Information("Autenticación exitosa: usuario {Username}, rol {Role}",
                        usuario.NombreUsuario, usuario.Rol);

                    
                    var mainWindow = new MainWindow(
                        usuario,
                        _etiquetadoService,
                        _usuarioService,
                        _printService,
                        _turnoService,
                        _logService,
                        _etiquetaPreviewService,
                        _lineaService,
                        _barcodeService,
                        _julianDateService,
                        _configuration,
                        _kioskManager); 

                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    _logService.Warning("Autenticación fallida: usuario {Username}", nombreUsuario);
                    txtError.Text = "Usuario o contraseña incorrectos";
                    ResetLoginControls();
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error durante la autenticación: usuario {Username}", nombreUsuario);
                txtError.Text = $"Error al iniciar sesión: {ex.Message}";
                ResetLoginControls();
            }
        }

        private void ResetLoginControls()
        {
            loginProgressBar.Visibility = Visibility.Collapsed;
            btnLogin.IsEnabled = true;
            txtPassword.Password = string.Empty;
            txtPassword.Focus();
        }
    }
}