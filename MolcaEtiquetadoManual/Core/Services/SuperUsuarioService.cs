// Core/Services/SuperUsuarioService.cs
using System;
using System.Linq;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class SuperUsuarioService : ISuperUsuarioService
    {
        private readonly ILogService _logService;
        private const string SUPER_USUARIO_NOMBRE = "ketan";

        public SuperUsuarioService(ILogService logService)
        {
            _logService = logService;
        }

        public Usuario ValidarSuperUsuario(string nombreUsuario, string contraseña)
        {
            try
            {
                // Verificar si es el nombre de super usuario
                if (!nombreUsuario?.Equals(SUPER_USUARIO_NOMBRE, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return null;
                }

                // Obtener la contraseña esperada para hoy
                string contraseñaEsperada = GenerarContraseñaDelDia();

                // Validar contraseña
                if (contraseña == contraseñaEsperada)
                {
                    _logService.Information("Autenticación exitosa del super usuario: {Username}", nombreUsuario);

                    // Crear usuario super administrador temporal
                    return new Usuario
                    {
                        Id = -1, // ID especial para super usuario
                        NombreUsuario = SUPER_USUARIO_NOMBRE,
                        Contraseña = contraseñaEsperada,
                        Rol = "Super Administrador",
                        Activo = true
                    };
                }
                else
                {
                    _logService.Warning("Intento fallido de autenticación del super usuario con contraseña incorrecta");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al validar super usuario");
                return null;
            }
        }

        public string ObtenerContraseñaActual()
        {
            return GenerarContraseñaDelDia();
        }

        public bool EsSuperUsuario(string nombreUsuario)
        {
            return nombreUsuario?.Equals(SUPER_USUARIO_NOMBRE, StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Genera la contraseña del super usuario basada en la fecha actual
        /// La contraseña es la fecha actual en formato "ddMMyy" al revés
        /// Ejemplo: Si hoy es 22/05/25, la contraseña será "520522"
        /// </summary>
        private string GenerarContraseñaDelDia()
        {
            try
            {
                // Obtener fecha actual
                DateTime fechaActual = DateTime.Now;

                // Formatear como ddMMyy
                string fechaFormateada = fechaActual.ToString("ddMMyy");

                // Invertir la cadena (fecha al revés)
                string contraseña = new string(fechaFormateada.Reverse().ToArray());

                _logService.Debug("Contraseña del super usuario generada para la fecha {Fecha}: {Password}",
                    fechaActual.ToString("dd/MM/yyyy"), contraseña);

                return contraseña;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al generar contraseña del super usuario");

                // Contraseña de respaldo en caso de error
                return "112233";
            }
        }

        /// <summary>
        /// Método adicional para generar contraseña de una fecha específica (útil para testing)
        /// </summary>
        public string GenerarContraseñaParaFecha(DateTime fecha)
        {
            try
            {
                string fechaFormateada = fecha.ToString("ddMMyy");
                return new string(fechaFormateada.Reverse().ToArray());
            }
            catch
            {
                return "112233";
            }
        }
    }
}