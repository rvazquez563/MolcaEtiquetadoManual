using System;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Core.Interfaces
{
    public interface ILogService
    {
        void Debug(string message);
        void Debug(string message, params object[] args);
        void Debug(Exception exception, string message, params object[] args);

        void Information(string message);
        void Information(string message, params object[] args);
        void Information(Exception exception, string message, params object[] args);

        void Warning(string message);
        void Warning(string message, params object[] args);
        void Warning(Exception exception, string message, params object[] args);

        void Error(string message);
        void Error(string message, params object[] args);
        void Error(Exception exception, string message, params object[] args);

        void Fatal(string message);
        void Fatal(string message, params object[] args);
        void Fatal(Exception exception, string message, params object[] args);
        void LogToUI(Action<ActivityLogItem> logAction, string message, ActivityLogItem.LogLevel level);
    }
}