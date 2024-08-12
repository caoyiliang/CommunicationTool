using System.Globalization;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    class TransferDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (TransferDirection)value == TransferDirection.Request ? " <- " : " -> ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
