using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Training.BusinessLogic.Gebucht;

namespace TrainingDisplay.Services;

public class TypToForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Anzeige item)
        {
            return Brushes.Black;
        }

        if (item.Typ.StartsWith(("Train")))
            return Brushes.Red;
        else if (item.Typ.StartsWith("Sport"))
            return Brushes.Green;
        else
            return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}