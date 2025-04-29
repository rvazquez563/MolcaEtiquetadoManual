using System;

namespace MolcaEtiquetadoManual.Core.Models
{
    public class ActivityLogItem
    {
        public string Description { get; set; }
        public string Time { get; set; }
        public LogLevel Level { get; set; }

        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Debug
        }
    }
}