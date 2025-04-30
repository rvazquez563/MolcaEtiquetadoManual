// Data/Repositories/UsuarioRepository.cs
// Actualizar la clase UsuarioRepository para agregar el método de eliminación

using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Context;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MolcaEtiquetadoManual.Data.Repositories
{
    public class UsuarioRepository
    {
        private readonly AppDbContext _context;

        public UsuarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public Usuario GetByCredentials(string nombreUsuario, string contraseña)
        {
            return _context.Usuarios
                .FirstOrDefault(u => u.NombreUsuario == nombreUsuario && u.Contraseña == contraseña && u.Activo);
        }

        public List<Usuario> GetAll()
        {
            // Ordenar la lista: primero los activos, luego por rol (Administradores primero) y luego por nombre
            return _context.Usuarios
                .OrderByDescending(u => u.Activo)
                .ThenBy(u => u.Rol != "Administrador") // Los administradores primero
                .ThenBy(u => u.NombreUsuario)
                .ToList();
        }

        public Usuario GetById(int id)
        {
            return _context.Usuarios.Find(id);
        }

        public Usuario GetByNombreUsuario(string nombreUsuario)
        {
            return _context.Usuarios
                .FirstOrDefault(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower());
        }

        public void Add(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            _context.SaveChanges();
        }

        public void Update(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var usuario = GetById(id);
            if (usuario != null)
            {
                // Eliminación lógica: marcar como inactivo en vez de eliminar físicamente
                usuario.Activo = false;
                _context.Usuarios.Update(usuario);
                _context.SaveChanges();
            }
        }

        public void DeletePhysical(int id)
        {
            var usuario = GetById(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                _context.SaveChanges();
            }
        }
    }
}
