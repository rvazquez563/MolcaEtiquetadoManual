// Core/Services/JulianDateService.cs
using System;
using System.Globalization;
using MolcaEtiquetadoManual.Core.Interfaces;

namespace MolcaEtiquetadoManual.Core.Services
{
    /// <summary>
    /// Servicio para la conversión de fechas al formato juliano usado en el sistema JDE
    /// </summary>
    public class JulianDateService : IJulianDateService
    {
        private readonly ILogService _logService;

        public JulianDateService(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Convierte una fecha DateTime al formato de fecha juliana de JDE
        /// Formato Juliana JDE: CYYDDD donde C=siglo, YY=año, DDD=día del año
        /// Ejemplo: 114001 = 1 de enero de 2014 (1=siglo XXI, 14=año 2014, 001=día 1 del año)
        /// </summary>
        public string ConvertirAFechaJuliana(DateTime fecha)
        {
            try
            {
                // Obtener el siglo (1 para 2000s, 0 para 1900s)
                int siglo = fecha.Year >= 2000 ? 1 : 0;

                // Obtener los dos últimos dígitos del año
                int año = fecha.Year % 100;

                // Obtener el día del año (1-366)
                int diaDelAño = fecha.DayOfYear;

                // Formatear como CYYDDD
                string fechaJuliana = $"{siglo}{año:D2}{diaDelAño:D3}";

                _logService.Debug("Fecha {0} convertida a juliana: {1}", fecha.ToString("dd/MM/yyyy"), fechaJuliana);
                return fechaJuliana;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al convertir fecha a formato juliano: {0}", fecha);
                throw new Exception($"Error al convertir fecha a formato juliano: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convierte una fecha en formato juliano de JDE a un objeto DateTime
        /// </summary>
        public DateTime ConvertirDesdeFechaJuliana(string fechaJuliana)
        {
            try
            {
                if (string.IsNullOrEmpty(fechaJuliana) || fechaJuliana.Length != 6)
                {
                    throw new ArgumentException("La fecha juliana debe tener 6 dígitos en formato CYYDDD");
                }

                // Extraer componentes
                int siglo = int.Parse(fechaJuliana.Substring(0, 1));
                int año = int.Parse(fechaJuliana.Substring(1, 2));
                int diaDelAño = int.Parse(fechaJuliana.Substring(3, 3));

                // Calcular el año completo
                int añoCompleto = (siglo == 1 ? 2000 : 1900) + año;

                // Crear fecha base (1 de enero del año)
                DateTime fechaBase = new DateTime(añoCompleto, 1, 1);

                // Sumar días
                DateTime fechaResultado = fechaBase.AddDays(diaDelAño - 1);

                _logService.Debug("Fecha juliana {0} convertida a: {1}", fechaJuliana, fechaResultado.ToString("dd/MM/yyyy"));
                return fechaResultado;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al convertir desde formato juliano: {0}", fechaJuliana);
                throw new Exception($"Error al convertir desde formato juliano: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convierte un string con formato DDMMYY a formato de fecha juliana
        /// </summary>
        public string ConvertirDesdeFechaCorta(string fechaCorta)
        {
            try
            {
                // Validar formato
                if (string.IsNullOrEmpty(fechaCorta) || fechaCorta.Length != 6)
                {
                    throw new ArgumentException("La fecha debe tener formato DDMMYY");
                }

                // Extraer componentes
                int dia = int.Parse(fechaCorta.Substring(0, 2));
                int mes = int.Parse(fechaCorta.Substring(2, 2));
                int año = 2000 + int.Parse(fechaCorta.Substring(4, 2));

                // Crear DateTime
                DateTime fecha = new DateTime(año, mes, dia);

                // Convertir a juliana
                return ConvertirAFechaJuliana(fecha);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al convertir fecha corta a formato juliano: {0}", fechaCorta);
                throw new Exception($"Error al convertir fecha corta a formato juliano: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convierte un timestamp HHMMSS a formato de hora juliana (HHMMSS)
        /// </summary>
        public string ConvertirAHoraJuliana(string hora)
        {
            // En este caso, el formato es el mismo, solo validamos
            if (string.IsNullOrEmpty(hora) || hora.Length != 6)
            {
                throw new ArgumentException("La hora debe tener formato HHMMSS");
            }

            return hora;
        }

        /// <summary>
        /// Convierte una hora DateTime a formato HHMMSS
        /// </summary>
        public string ConvertirAHoraJuliana(DateTime fecha)
        {
            return fecha.ToString("HHmmss");
        }
    }


}