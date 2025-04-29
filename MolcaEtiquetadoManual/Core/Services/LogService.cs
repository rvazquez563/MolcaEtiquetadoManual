using Microsoft.Extensions.Logging;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using System;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class LogService : ILogService
    {
        private readonly ILogger<LogService> _logger;

        public LogService(ILogger<LogService> logger)
        {
            _logger = logger;
        }

        public void Debug(string message)
        {
            _logger.LogDebug(message);
        }

        public void Debug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }

        public void Debug(Exception exception, string message, params object[] args)
        {
            _logger.LogDebug(exception, message, args);
        }

        public void Information(string message)
        {
            _logger.LogInformation(message);
        }

        public void Information(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void Information(Exception exception, string message, params object[] args)
        {
            _logger.LogInformation(exception, message, args);
        }

        public void Warning(string message)
        {
            _logger.LogWarning(message);
        }

        public void Warning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void Warning(Exception exception, string message, params object[] args)
        {
            _logger.LogWarning(exception, message, args);
        }

        public void Error(string message)
        {
            _logger.LogError(message);
        }

        public void Error(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void Error(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }

        public void Fatal(string message)
        {
            _logger.LogCritical(message);
        }

        public void Fatal(string message, params object[] args)
        {
            _logger.LogCritical(message, args);
        }

        public void Fatal(Exception exception, string message, params object[] args)
        {
            _logger.LogCritical(exception, message, args);
        }
        public void LogToUI(Action<ActivityLogItem> logAction, string message, ActivityLogItem.LogLevel level)
        {
            // Registra en Serilog según el nivel
            switch (level)
            {
                case ActivityLogItem.LogLevel.Debug:
                    _logger.LogDebug(message);
                    break;
                case ActivityLogItem.LogLevel.Info:
                    _logger.LogInformation(message);
                    break;
                case ActivityLogItem.LogLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case ActivityLogItem.LogLevel.Error:
                    _logger.LogError(message);
                    break;
            }

            // Crea y devuelve un ActivityLogItem para la UI
            var logItem = new ActivityLogItem
            {
                Description = message,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Level = level
            };

            // Ejecuta la acción con el item de log
            logAction(logItem);
        }
    }
}