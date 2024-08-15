﻿using System.Globalization;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class LengthTbShowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Config.DataType)value == Config.DataType.固定长度 ? "Visible" : "Collapsed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
