// Core/Services/UsuarioService.cs - Versión modificada
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Repositories;
using System.Collections.Generic;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly UsuarioRepository _repository;
        private readonly ILogService _logService;
        private readonly ISuperUsuarioService _superUsuarioService; // ✅ NUEVO

        public UsuarioService(UsuarioRepository repository, ILogService logService = null, ISuperUsuarioService superUsuarioService = null)
        {
            _repository = repository;
            _logService = logService;
            _superUsuarioService = superUsuarioService; // ✅ NUEVO
        }

        public Usuario Authenticate(string nombreUsuario, string contraseña)
        {
            try
            {
                // ✅ NUEVA LÓGICA: Primero verificar si es el super usuario
                if (_superUsuarioService != null && _superUsuarioService.EsSuperUsuario(nombreUsuario))
                {
                    var superUsuario = _superUsuarioService.ValidarSuperUsuario(nombreUsuario, contraseña);
                    if (superUsuario != null)
                    {
                        _logService?.Information("Autenticación exitosa para SUPER USUARIO: {Username}", nombreUsuario);
                        return superUsuario;
                    }
                    else
                    {
                        _logService?.Warning("Intento fallido de autenticación para super usuario: {Username}", nombreUsuario);
                        return null;
                    }
                }

                // Lógica original para usuarios normales
                var usuario = _repository.GetByCredentials(nombreUsuario, contraseña);

                if (usuario != null && _logService != null)
                {
                    _logService.Information("Autenticación exitosa para usuario: {Username}", nombreUsuario);
                }
                else if (_logService != null)
                {
                    _logService.Warning("Intento fallido de autenticación para usuario: {Username}", nombreUsuario);
                }

                return usuario;
            }
            catch (System.Exception ex)
            {
                _logService?.Error(ex, "Error durante la autenticación del usuario: {Username}", nombreUsuario);
                return null;
            }
        }

        // ✅ NUEVO MÉTODO: Verificar si un usuario es super usuario
        public bool EsSuperUsuario(Usuario usuario)
        {
            return usuario != null &&
                   _superUsuarioService != null &&
                   _superUsuarioService.EsSuperUsuario(usuario.NombreUsuario);
        }

        // ✅ NUEVO MÉTODO: Obtener información del super usuario para mostrar en UI
        public string ObtenerInfoSuperUsuario()
        {
            if (_superUsuarioService != null)
            {
                return $"Super Usuario: ketan | Contraseña hoy: {_superUsuarioService.ObtenerContraseñaActual()}";
            }
            return "Super Usuario no disponible";
        }

        // Métodos existentes sin cambios
        public List<Usuario> GetAllUsuarios()
        {
            return _repository.GetAll();
        }

        public Usuario GetUsuarioById(int id)
        {
            return _repository.GetById(id);
        }

        public Usuario GetUsuarioByNombreUsuario(string nombreUsuario)
        {
            return _repository.GetByNombreUsuario(nombreUsuario);
        }

        public void AddUsuario(Usuario usuario)
        {
            if (_logService != null)
            {
                _logService.Information("Creando nuevo usuario: {Username}, Rol: {Rol}",
                    usuario.NombreUsuario, usuario.Rol);
            }

            _repository.Add(usuario);
        }

        public void UpdateUsuario(Usuario usuario)
        {
            if (_logService != null)
            {
                _logService.Information("Actualizando usuario: {Username}, ID: {Id}",
                    usuario.NombreUsuario, usuario.Id);
            }

            _repository.Update(usuario);
        }

        public void DeleteUsuario(int id)
        {
            var usuario = GetUsuarioById(id);
            if (usuario != null && _logService != null)
            {
                _logService.Information("Eliminando usuario (lógico): {Username}, ID: {Id}",
                    usuario.NombreUsuario, usuario.Id);
            }

            _repository.Delete(id);
        }

        public bool ExisteNombreUsuario(string nombreUsuario, int? idExcluir = null)
        {
            // ✅ NUEVA VALIDACIÓN: No permitir crear usuarios con el nombre del super usuario
            if (_superUsuarioService != null && _superUsuarioService.EsSuperUsuario(nombreUsuario))
            {
                return true; // Consideramos que "existe" para evitar su creación
            }

            var usuario = _repository.GetByNombreUsuario(nombreUsuario);

            // Si el usuario existe y no es el que estamos excluyendo (para edición)
            return usuario != null && (idExcluir == null || usuario.Id != idExcluir);
        }
    }
}