using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Core.Services;
using System.Windows;
using System;

namespace MolcaEtiquetadoManual.UI.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IEtiquetadoService _etiquetadoService;
        private readonly IPrintService _printService;
        private readonly ITurnoService _turnoService;
        private readonly ILogService _logService;

        public LoginWindow(IUsuarioService usuarioService, IEtiquetadoService etiquetadoService,
            IPrintService printService, ITurnoService turnoService, ILogService logService)
        {
            InitializeComponent();
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _etiquetadoService = etiquetadoService ?? throw new ArgumentNullException(nameof(etiquetadoService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _turnoService = turnoService ?? throw new ArgumentNullException(nameof(turnoService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));

            _logService.Information("Ventana de login iniciada");
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string nombreUsuario = txtUsername.Text;
            string contraseña = txtPassword.Password;

            if (string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(contraseña))
            {
                _logService.Warning("Intento de login con campos vacíos");
                txtError.Text = "Por favor, ingrese nombre de usuario y contraseña";
                return;
            }

            try
            {
                _logService.Information("Intento de autenticación: usuario {Username}", nombreUsuario);
                var usuario = _usuarioService.Authenticate(nombreUsuario, contraseña);

                if (usuario != null)
                {
                    _logService.Information("Autenticación exitosa: usuario {Username}, rol {Role}",
                        usuario.NombreUsuario, usuario.Rol);

                    // Abrir ventana principal con el servicio de etiquetado
                    var mainWindow = new MainWindow(usuario, _etiquetadoService, _usuarioService,
                        _printService, _turnoService, _logService);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    _logService.Warning("Autenticación fallida: usuario {Username}", nombreUsuario);
                    txtError.Text = "Usuario o contraseña incorrectos";
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error durante la autenticación: usuario {Username}", nombreUsuario);
                txtError.Text = $"Error al iniciar sesión: {ex.Message}";
            }
        }
    }
}