// Core/Interfaces/IUsuarioService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    // Core/Interfaces/IUsuarioService.cs
    public interface IUsuarioService
    {
        Usuario Authenticate(string nombreUsuario, string contraseña);
        List<Usuario> GetAllUsuarios();
        Usuario GetUsuarioById(int id);
        Usuario GetUsuarioByNombreUsuario(string nombreUsuario);
        void AddUsuario(Usuario usuario);
        void UpdateUsuario(Usuario usuario);
        void DeleteUsuario(int id);
        bool ExisteNombreUsuario(string nombreUsuario, int? idExcluir = null);
    }
}