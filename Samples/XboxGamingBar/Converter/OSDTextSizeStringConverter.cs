using System;
using Windows.UI.Xaml.Data;

namespace XboxGamingBar.Converter
{
    internal class OSDTextSizeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double d)
            {
                int val = (int)Math.Round(d);
                if (val == 100) return "100% (Auto)";
                return $"{val}%";
            }
            if (value is int i)
            {
                if (i == 100) return "100% (Auto)";
                return $"{i}%";
            }
            return $"{value}%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
