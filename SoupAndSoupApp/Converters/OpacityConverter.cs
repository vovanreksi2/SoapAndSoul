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

        if (value is not bool isPointerOver) return value;

        if (parameter is not string param)
            return 0;

        if (param.Equals("From0.4To0", StringComparison.OrdinalIgnoreCase))
            return isPointerOver ? 0 : MinimalAllowOpacity;

        if (param.Equals("From0To1", StringComparison.OrdinalIgnoreCase))
            return isPointerOver ?  1: 0;

        if (param.Equals("From1To0.4", StringComparison.OrdinalIgnoreCase))
            return isPointerOver ?  0.4 : 1;

        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}