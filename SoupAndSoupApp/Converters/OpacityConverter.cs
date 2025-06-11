using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SoupAndSoupApp.Converters;

public class OpacityConverter : IValueConverter
{
    public static readonly OpacityConverter Instance = new();

    private const double MinimalAllowOpacity = 0.4;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return 1;

        if (value is bool b)
        {
            return b ? MinimalAllowOpacity : 1;
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}