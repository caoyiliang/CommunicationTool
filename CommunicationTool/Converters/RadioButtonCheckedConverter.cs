using Config;
using System.Globalization;
using System.Windows.Data;

namespace CommunicationTool.Converters
{
    internal class RadioButtonCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (DataType)value == (DataType)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value && Enum.TryParse(typeof(DataType), parameter.ToString(), out var result))
            {
                return result;
            }
            return Binding.DoNothing;
        }
    }
}
