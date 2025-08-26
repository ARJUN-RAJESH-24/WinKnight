using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WinKnightUI
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)) : new SolidColorBrush(Color.FromArgb(255, 52, 152, 219));
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}