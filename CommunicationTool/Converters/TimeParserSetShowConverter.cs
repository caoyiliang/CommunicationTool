using Config;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class TimeParserSetShowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ParserType)value) == ParserType.TimeParser ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
