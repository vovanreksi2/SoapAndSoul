using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace SoupAndSoupApp.Converters;

public class AndConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.OfType<bool>().All(v => v);
    }
}

public class TextVisibilityConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return false;

        var isButtonShow = values[0] as bool? ?? false;
        var isChecked = values[1] as bool? ?? false;

        if (isButtonShow)
            return !isChecked;
            
        return true;
    }
}