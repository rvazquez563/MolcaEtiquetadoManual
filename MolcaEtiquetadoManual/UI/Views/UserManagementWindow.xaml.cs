// UI/Views/UserManagementWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.UI.Views
{
    public partial class UserManagementWindow : Window
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogService _logService;
        private readonly Usuario _currentUser;
        private Usuario _selectedUser;
        private bool _isEditing = false;

        public UserManagementWindow(Usuario currentUser, IUsuarioService usuarioService, ILogService logService)
        {
            InitializeComponent();

            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));

            // Verificar si el usuario tiene permisos de administrador
            if (_currentUser.Rol != "Administrador")
            {
                _logService.Warning("Usuario {Username} sin permisos intentó acceder a gestión de usuarios", _currentUser.NombreUsuario);
                MessageBox.Show("Solo los administradores pueden acceder a la gestión de usuarios.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            // Cargar lista de usuarios
            CargarUsuarios();

            // Inicializar el formulario en modo de agregar
            InicializarFormulario();

            _logService.Information("Ventana de gestión de usuarios abierta por {Username}", _currentUser.NombreUsuario);
        }

        private void CargarUsuarios()
        {
            try
            {
                var usuarios = _usuarioService.GetAllUsuarios();
                lvUsuarios.ItemsSource = usuarios;

                if (usuarios.Count > 0)
                {
                    txtEstado.Text = $"Total de usuarios: {usuarios.Count}";
                }
                else
                {
                    txtEstado.Text = "No hay usuarios en el sistema";
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al cargar usuarios");
                MessageBox.Show($"Error al cargar la lista de usuarios: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InicializarFormulario()
        {
            // Limpiar campos
            txtNombreUsuario.Text = string.Empty;
            txtContraseña.Password = string.Empty;
            txtConfirmarContraseña.Password = string.Empty;
            cmbRol.SelectedIndex = 1; // Operador por defecto
            chkActivo.IsChecked = true;
            txtError.Text = string.Empty;

            // Establecer como modo de agregar
            _isEditing = false;
            _selectedUser = null;
            txtTituloForm.Text = "Agregar Usuario";
            txtContraseña.IsEnabled = true;
            txtConfirmarContraseña.IsEnabled = true;
        }

        private void PrepararEdicion(Usuario usuario)
        {
            if (usuario == null) return;

            // Verificar si es el mismo usuario actual
            if (usuario.Id == _currentUser.Id)
            {
                MessageBox.Show("No puede editar su propio usuario desde esta pantalla.",
                    "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _selectedUser = usuario;
            _isEditing = true;

            // Llenar formulario
            txtNombreUsuario.Text = usuario.NombreUsuario;
            txtContraseña.Password = string.Empty; // No mostramos la contraseña actual
            txtConfirmarContraseña.Password = string.Empty;

            // Seleccionar el rol
            if (usuario.Rol == "Administrador")
                cmbRol.SelectedIndex = 0;
            else
                cmbRol.SelectedIndex = 1;

            chkActivo.IsChecked = usuario.Activo;
            txtError.Text = string.Empty;

            // Actualizar título y estado de campos
            txtTituloForm.Text = $"Editar Usuario: {usuario.NombreUsuario}";

            // Al editar, las contraseñas son opcionales (solo si se desea cambiar)
            txtContraseña.IsEnabled = true;
            txtConfirmarContraseña.IsEnabled = true;
        }

        private void GuardarUsuario()
        {
            // Validar datos
            if (string.IsNullOrWhiteSpace(txtNombreUsuario.Text))
            {
                txtError.Text = "El nombre de usuario es obligatorio";
                txtNombreUsuario.Focus();
                return;
            }

            // Si es nuevo o se desea cambiar la contraseña
            if (!_isEditing || !string.IsNullOrEmpty(txtContraseña.Password))
            {
                if (string.IsNullOrWhiteSpace(txtContraseña.Password))
                {
                    txtError.Text = "La contraseña es obligatoria";
                    txtContraseña.Focus();
                    return;
                }

                if (txtContraseña.Password != txtConfirmarContraseña.Password)
                {
                    txtError.Text = "Las contraseñas no coinciden";
                    txtConfirmarContraseña.Focus();
                    return;
                }

                if (txtContraseña.Password.Length < 6)
                {
                    txtError.Text = "La contraseña debe tener al menos 6 caracteres";
                    txtContraseña.Focus();
                    return;
                }
            }

            if (cmbRol.SelectedItem == null)
            {
                txtError.Text = "Debe seleccionar un rol";
                cmbRol.Focus();
                return;
            }

            try
            {
                string nombreUsuario = txtNombreUsuario.Text.Trim();
                string rol = ((ComboBoxItem)cmbRol.SelectedItem).Content.ToString();
                bool activo = chkActivo.IsChecked ?? true;

                // Verificar si el nombre de usuario ya existe (solo para nuevos usuarios)
                if (!_isEditing)
                {
                    var usuarioExistente = _usuarioService.GetAllUsuarios().FirstOrDefault(u =>
                        u.NombreUsuario.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase));

                    if (usuarioExistente != null)
                    {
                        txtError.Text = "El nombre de usuario ya existe";
                        txtNombreUsuario.Focus();
                        return;
                    }
                }

                if (_isEditing)
                {
                    // Actualizar usuario existente
                    _selectedUser.NombreUsuario = nombreUsuario;

                    // Solo actualizar contraseña si se proporcionó una nueva
                    if (!string.IsNullOrEmpty(txtContraseña.Password))
                    {
                        _selectedUser.Contraseña = txtContraseña.Password;
                    }

                    _selectedUser.Rol = rol;
                    _selectedUser.Activo = activo;

                    _usuarioService.UpdateUsuario(_selectedUser);
                    _logService.Information("Usuario actualizado: {Username} ({Id})", _selectedUser.NombreUsuario, _selectedUser.Id);

                    txtEstado.Text = $"Usuario '{_selectedUser.NombreUsuario}' actualizado correctamente";
                }
                else
                {
                    // Crear nuevo usuario
                    var nuevoUsuario = new Usuario
                    {
                        NombreUsuario = nombreUsuario,
                        Contraseña = txtContraseña.Password,
                        Rol = rol,
                        Activo = activo
                    };

                    _usuarioService.AddUsuario(nuevoUsuario);
                    _logService.Information("Nuevo usuario creado: {Username}", nuevoUsuario.NombreUsuario);

                    txtEstado.Text = $"Usuario '{nuevoUsuario.NombreUsuario}' creado correctamente";
                }

                // Recargar lista y limpiar formulario
                CargarUsuarios();
                InicializarFormulario();
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al guardar usuario");
                MessageBox.Show($"Error al guardar el usuario: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EliminarUsuario(Usuario usuario)
        {
            if (usuario == null) return;

            // No permitir eliminar el usuario actual
            if (usuario.Id == _currentUser.Id)
            {
                MessageBox.Show("No puede eliminar su propio usuario.",
                    "Operación no permitida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Confirmar eliminación
            var result = MessageBox.Show($"¿Está seguro que desea eliminar el usuario '{usuario.NombreUsuario}'?",
                "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // En vez de eliminar físicamente, marcamos como inactivo
                    usuario.Activo = false;
                    _usuarioService.UpdateUsuario(usuario);

                    _logService.Information("Usuario desactivado: {Username} ({Id})", usuario.NombreUsuario, usuario.Id);
                    txtEstado.Text = $"Usuario '{usuario.NombreUsuario}' desactivado correctamente";

                    // Recargar lista y limpiar formulario
                    CargarUsuarios();
                    InicializarFormulario();
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al desactivar usuario");
                    MessageBox.Show($"Error al desactivar el usuario: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #region Event Handlers

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LvUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedUser = lvUsuarios.SelectedItem as Usuario;

            // Habilitar/deshabilitar botones según la selección
            btnEliminar.IsEnabled = _selectedUser != null && _selectedUser.Id != _currentUser.Id;
            btnEditar.IsEnabled = _selectedUser != null;
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            var usuario = lvUsuarios.SelectedItem as Usuario;
            if (usuario != null)
            {
                PrepararEdicion(usuario);
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            var usuario = lvUsuarios.SelectedItem as Usuario;
            if (usuario != null)
            {
                EliminarUsuario(usuario);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            InicializarFormulario();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            GuardarUsuario();
        }

        #endregion
    }
}