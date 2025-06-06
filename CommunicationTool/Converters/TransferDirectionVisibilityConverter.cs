using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class TransferDirectionVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((TransferDirection)values[0] == TransferDirection.Request)
            {
                if ((bool)values[1])
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
