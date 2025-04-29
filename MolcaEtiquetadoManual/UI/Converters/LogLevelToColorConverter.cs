using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.UI.Converters
{
    public class LogLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ActivityLogItem.LogLevel level)
            {
                switch (level)
                {
                    case ActivityLogItem.LogLevel.Debug:
                        return new SolidColorBrush(Colors.Gray);
                    case ActivityLogItem.LogLevel.Info:
                        return new SolidColorBrush(Colors.Black);
                    case ActivityLogItem.LogLevel.Warning:
                        return new SolidColorBrush(Colors.DarkOrange);
                    case ActivityLogItem.LogLevel.Error:
                        return new SolidColorBrush(Colors.Red);
                    default:
                        return new SolidColorBrush(Colors.Black);
                }
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // No necesitamos la conversión inversa
            throw new NotImplementedException();
        }
    }
}