using System.Globalization;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class SerialPortConnectStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "关闭串口" : "打开串口";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
