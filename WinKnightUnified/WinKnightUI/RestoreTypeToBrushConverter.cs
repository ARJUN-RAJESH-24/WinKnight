using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WinKnightUI
{
    public class RestoreTypeToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush ManualBrush = new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Blue
        private static readonly SolidColorBrush AutomaticBrush = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(32, 52, 78)); // Default gray

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return DefaultBrush;

            string sourceType = value.ToString().ToLower();

            return sourceType switch
            {
                "manual" => ManualBrush,
                "automatic" => AutomaticBrush,
                _ => DefaultBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
