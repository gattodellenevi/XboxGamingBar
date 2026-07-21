using System;
using Windows.UI.Xaml.Data;

namespace XboxGamingBar.Converter
{
    internal class FPSStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return $"{value} FPS";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
