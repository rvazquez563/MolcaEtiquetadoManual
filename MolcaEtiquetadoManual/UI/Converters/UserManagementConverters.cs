// UI/Converters/UserManagementConverters.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MolcaEtiquetadoManual.UI.Converters
{
    /// <summary>
    /// Convierte un valor booleano (Activo) a texto descriptivo ("Activo"/"Inactivo")
    /// </summary>
    public class EstadoUsuarioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool activo)
            {
                return activo ? "Activo" : "Inactivo";
            }
            return "Desconocido";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte un valor booleano (Activo) a un color (Verde/Rojo)
    /// </summary>
    public class EstadoUsuarioColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool activo)
            {
                return activo
                    ? new SolidColorBrush(Colors.Green)
                    : new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte un objeto a booleano (para habilitar/deshabilitar botones basado en selección)
    /// </summary>
    public class ObjectToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}