using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace SoupAndSoupApp.Converters;

public class AndConverter : IMultiValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var result =  values.OfType<bool>().All(v => v);
        return Invert ? !result : result;
    }
}