using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Training.BusinessLogic.Gebucht;

namespace TrainingDisplay.Services;

public class IndexToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Anzeige item)
        {
            return Brushes.White;
        }
        
        return item.Index % 2 == 0 ? Brushes.White : Brushes.WhiteSmoke;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}