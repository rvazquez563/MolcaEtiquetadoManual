// UI/Views/LoginWindow.xaml.cs
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

        public LoginWindow(IUsuarioService usuarioService, IEtiquetadoService etiquetadoService, IPrintService printService,ITurnoService turnoService)
        {
            InitializeComponent();
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _etiquetadoService = etiquetadoService ?? throw new ArgumentNullException(nameof(etiquetadoService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _turnoService = turnoService ?? throw new ArgumentNullException(nameof(turnoService)); ; // Inicializa el servicio de turnos


        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string nombreUsuario = txtUsername.Text;
            string contraseña = txtPassword.Password;

            if (string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(contraseña))
            {
                txtError.Text = "Por favor, ingrese nombre de usuario y contraseña";
                return;
            }

            try
            {
                var usuario = _usuarioService.Authenticate(nombreUsuario, contraseña);

                if (usuario != null)
                {
                    // Abrir ventana principal con el servicio de etiquetado
                    var mainWindow = new MainWindow(usuario, _etiquetadoService, _usuarioService, _printService,_turnoService);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    txtError.Text = "Usuario o contraseña incorrectos";
                }
            }
            catch (Exception ex)
            {
                txtError.Text = $"Error al iniciar sesión: {ex.Message}";
            }
        }
    }
}