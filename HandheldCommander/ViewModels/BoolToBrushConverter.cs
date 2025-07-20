using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace HandheldCommander.ViewModels
{
    public class BoolToBrushConverter : IValueConverter
    {
        public IBrush SelectedBrush { get; set; } = Brushes.DeepSkyBlue;
        public IBrush UnselectedBrush { get; set; } = Brushes.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? SelectedBrush : UnselectedBrush;
            return UnselectedBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 