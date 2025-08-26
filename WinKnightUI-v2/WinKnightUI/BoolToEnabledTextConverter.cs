using System;
using System.Globalization;
using System.Windows.Data;

namespace WinKnightUI
{
    public class BoolToEnabledTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? "Disable" : "Enable";
            }
            return "Toggle";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}