using System;
using System.Globalization;
using System.Windows.Data;

namespace Surface_Editor.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool saved = (bool)value;
            return saved ? "Saved" : "Unsaved";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
