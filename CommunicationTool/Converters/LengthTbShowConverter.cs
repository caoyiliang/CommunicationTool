using Config;
using System.Globalization;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class LengthTbShowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (LengthDataType)value == LengthDataType.固定长度 ? "Visible" : "Collapsed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
