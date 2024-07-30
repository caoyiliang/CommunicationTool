using System.Globalization;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class RadioButtonCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (RecType)value == (RecType)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value && Enum.TryParse(typeof(RecType), parameter.ToString(), out var result))
            {
                return result;
            }
            return Binding.DoNothing;
        }
    }
}
