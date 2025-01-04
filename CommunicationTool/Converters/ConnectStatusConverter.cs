using Config;
using System.Globalization;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class ConnectStatusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((TestType)values[0])
            {
                default:
                    return (bool)values[1] ? "关闭连接" : "打开连接";
                case TestType.SerialPort:
                    return (bool)values[1] ? "关闭串口" : "打开串口";
                case TestType.TcpServer:
                    return (bool)values[1] ? "关闭监听" : "监听";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
