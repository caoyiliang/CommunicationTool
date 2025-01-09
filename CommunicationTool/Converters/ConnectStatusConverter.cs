using Config;
using System.Globalization;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class ConnectStatusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (TestType)values[0] switch
            {
                TestType.SerialPort => (bool)values[1] ? "关闭串口" : "打开串口",
                TestType.TcpServer => (bool)values[1] ? "关闭监听" : "监听",
                TestType.ClassicBluetoothServer => (bool)values[1] ? "关闭监听" : "监听",
                _ => (bool)values[1] ? "关闭连接" : "打开连接",
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
