// Core/Interfaces/ISuperUsuarioService.cs
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    public interface ISuperUsuarioService
    {
        /// <summary>
        /// Valida las credenciales del super usuario
        /// </summary>
        /// <param name="nombreUsuario">Nombre de usuario</param>
        /// <param name="contraseña">Contraseña ingresada</param>
        /// <returns>Usuario super administrador si las credenciales son válidas, null en caso contrario</returns>
        Usuario ValidarSuperUsuario(string nombreUsuario, string contraseña);

        /// <summary>
        /// Obtiene la contraseña actual del super usuario (para propósitos de debugging/logging)
        /// </summary>
        /// <returns>Contraseña actual</returns>
        string ObtenerContraseñaActual();

        /// <summary>
        /// Verifica si un usuario es el super usuario
        /// </summary>
        /// <param name="nombreUsuario">Nombre de usuario a verificar</param>
        /// <returns>True si es el super usuario</returns>
        bool EsSuperUsuario(string nombreUsuario);
    }
}