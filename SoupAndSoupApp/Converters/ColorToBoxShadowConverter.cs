using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SoupAndSoupApp.Converters;

public class ColorToBoxShadowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color color || (targetType != typeof(BoxShadows) && targetType != typeof(object))) 
            return null;
        
        double blurRadius = 0; // Default blur if no parameter or invalid
        if (parameter == null)
            return new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 0,
                Blur = blurRadius, // Use the parsed blurRadius
                Spread = 0,
                Color = color
            });

        // Try to parse the parameter to a double for BlurRadius
        if (double.TryParse(parameter.ToString(), NumberStyles.Any, culture, out double parsedBlur))
        {
            blurRadius = parsedBlur;
        }
        else
        {
            // Handle cases where parsing fails, e.g., log a warning or use a default
            Console.WriteLine($"Warning: Could not parse converter parameter '{parameter}' to a double for BlurRadius.");
        }

        return new BoxShadows(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 0,
            Blur = blurRadius, // Use the parsed blurRadius
            Spread = 0,
            Color = color
        });
        // Return default or null for invalid input
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException(); // Not needed for this scenario
    }
}