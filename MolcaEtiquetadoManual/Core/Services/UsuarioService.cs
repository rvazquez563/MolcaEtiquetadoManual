// Core/Services/UsuarioService.cs
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Repositories;
using System.Collections.Generic;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly UsuarioRepository _repository;

        public UsuarioService(UsuarioRepository repository)
        {
            _repository = repository;
        }

        public Usuario Authenticate(string nombreUsuario, string contraseña)
        {
            return _repository.GetByCredentials(nombreUsuario, contraseña);
        }

        public List<Usuario> GetAllUsuarios()
        {
            return _repository.GetAll();
        }

        public Usuario GetUsuarioById(int id)
        {
            return _repository.GetById(id);
        }

        public void AddUsuario(Usuario usuario)
        {
            _repository.Add(usuario);
        }

        public void UpdateUsuario(Usuario usuario)
        {
            _repository.Update(usuario);
        }
    }
}
