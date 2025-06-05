// UI/Views/LoginWindow.xaml.cs - CORREGIDO
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
        private bool _isNormalClose = false;

        public LoginWindow(IUsuarioService usuarioService,
                         IEtiquetadoService etiquetadoService,
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
            _kioskManager = kioskManager;

            _kioskModeEnabled = _configuration?.GetSection("KioskSettings").GetValue<bool>("Enabled", false) ?? false;

            // Cargar versión desde configuración
            if (_configuration != null)
            {
                string version = _configuration.GetSection("AppSettings")["Version"] ?? "1.0.0";
                txtVersion.Text = $"v{version}";

                if (_kioskModeEnabled)
                {
                    txtVersion.Text += " - KIOSK";
                    this.Title += " - MODO KIOSK";
                }

#if DEBUG
                try
                {
                    var superUsuarioService = new SuperUsuarioService(_logService);
                    string contraseñaHoy = superUsuarioService.ObtenerContraseñaActual();
                    txtVersion.Text += $" | SU: ketan";
                    _logService.Information("Contraseña del super usuario para hoy: {Password}", contraseñaHoy);
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al mostrar información del super usuario");
                }
#endif
            }

            _logService.Information("Ventana de login iniciada - Modo Kiosk: {KioskMode}", _kioskModeEnabled);

            Loaded += LoginWindow_Loaded_Event;
        }

        private void LoginWindow_Loaded_Event(object sender, RoutedEventArgs e)
        {
            txtUsername.Focus();

            if (_kioskModeEnabled && _kioskManager != null)
            {
                try
                {
                    _logService.Information("Activando modo Kiosk en ventana de login");
                    _kioskManager.EnableKioskMode(this);
                    _logService.Information("Modo Kiosk activado exitosamente");
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al activar modo Kiosk");
                    MessageBox.Show($"Error al activar modo Kiosk: {ex.Message}",
                                   "Error Kiosk", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isNormalClose)
            {
                base.OnClosing(e);
                return;
            }

            if (_kioskModeEnabled)
            {
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
                    if (_kioskManager != null)
                    {
                        _kioskManager.DisableKioskMode();
                        _logService.Warning("Modo Kiosk deshabilitado por cierre de ventana de login");
                    }
                }
            }

            base.OnClosing(e);
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

                var usuario = await Task.Run(() => _usuarioService.Authenticate(nombreUsuario, contraseña));

                if (usuario != null)
                {
                    bool esSuperUsuario = usuario.Id == -1 || usuario.Rol == "Super Administrador";
                    if (esSuperUsuario)
                    {
                        _logService.Information("SUPER USUARIO autenticado: {Username}", usuario.NombreUsuario);

                        var result = MessageBox.Show(
                            $"¡Bienvenido Super Usuario!\n\n" +
                            $"Usuario: {usuario.NombreUsuario}\n" +
                            $"Acceso: Administrador Total\n" +
                            $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}\n\n" +
                            $"¿Continuar al sistema?",
                            "Super Usuario Detectado",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.No)
                        {
                            ResetLoginControls();
                            return;
                        }
                    }

                    _logService.Information("Autenticación exitosa: usuario {Username}, rol {Role}",
                        usuario.NombreUsuario, usuario.Rol);

                    // ✅ CAMBIO IMPORTANTE: Marcar como cierre normal ANTES de crear el MainWindow
                    _isNormalClose = true;

                    // ✅ CAMBIO IMPORTANTE: Ocultar el LoginWindow inmediatamente
                    this.Close();

                    try
                    {
                        // Crear MainWindow
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
                            _kioskManager,this);

                        // ✅ CAMBIO IMPORTANTE: Configurar MainWindow para pantalla completa ANTES de mostrarlo
                        if (_kioskModeEnabled)
                        {
                            mainWindow.WindowState = WindowState.Maximized;
                            mainWindow.WindowStyle = WindowStyle.None;
                            mainWindow.Topmost = true;
                        }
                        else
                        {
                            mainWindow.WindowState = WindowState.Maximized;
                        }

                        // ✅ CAMBIO IMPORTANTE: Asegurar que se muestre al frente
                        mainWindow.Show();
                        mainWindow.Activate();
                        mainWindow.Focus();

                        // ✅ CAMBIO IMPORTANTE: Cerrar LoginWindow después de que MainWindow esté visible
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.Close();
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                    catch (Exception ex)
                    {
                        _logService.Error(ex, "Error al crear MainWindow");
                        this.Show(); // Volver a mostrar LoginWindow si hay error
                        txtError.Text = $"Error al abrir ventana principal: {ex.Message}";
                        ResetLoginControls();
                    }
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