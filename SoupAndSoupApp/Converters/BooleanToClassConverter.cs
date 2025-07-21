using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SoupAndSoupApp.Converters;

public class BooleanToClassConverter : IValueConverter
{
    public string TrueClass { get; set; }
    public string FalseClass { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? TrueClass : FalseClass;
        }
        return FalseClass; // або throw new ArgumentException
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}