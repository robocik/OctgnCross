namespace Octgn.Utils.Converters;

using Avalonia.Data.Converters;
using System;
using System.Globalization;

public class IdToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte id && id == 0)
        {
            return false; 
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
