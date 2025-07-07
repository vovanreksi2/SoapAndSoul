using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SoupAndSoupApp.Converters;

public class EditableTextBlockMultiBindConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return false;

        var isEditing = values[0] as bool? ?? false;
        var isPointOver = values[1] as bool? ?? false;

        return isPointOver && !isEditing; // If not hovering, return false to prevent editing
    }
}