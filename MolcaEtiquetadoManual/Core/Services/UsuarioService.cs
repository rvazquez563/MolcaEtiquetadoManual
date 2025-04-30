// Core/Services/UsuarioService.cs
// Actualizar la clase UsuarioService para agregar los métodos de gestión de usuarios

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

        public UsuarioService(UsuarioRepository repository, ILogService logService = null)
        {
            _repository = repository;
            _logService = logService;
        }

        public Usuario Authenticate(string nombreUsuario, string contraseña)
        {
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
            var usuario = _repository.GetByNombreUsuario(nombreUsuario);

            // Si el usuario existe y no es el que estamos excluyendo (para edición)
            return usuario != null && (idExcluir == null || usuario.Id != idExcluir);
        }
    }
}