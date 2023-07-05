﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Surface_Editor.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool saved = (bool)value;
            return saved ? Brushes.LimeGreen : Brushes.OrangeRed;            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
