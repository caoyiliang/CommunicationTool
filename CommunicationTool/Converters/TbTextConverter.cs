using Config;
using System.Globalization;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class TbTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (DataType)value == DataType.固定长度 ? "固定长度:" : "高字节在前:";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
