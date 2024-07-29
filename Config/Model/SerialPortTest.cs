using CommunityToolkit.Mvvm.ComponentModel;

namespace Config.Model
{
    public partial class SerialPortTest : ObservableObject
    {
        [ObservableProperty]
        private Guid _Id;
        [ObservableProperty]
        private string? _TestName = nameof(SerialPortTest);
        [ObservableProperty]
        private SerialPortConnection _SerialPortConnection = new();
        [ObservableProperty]
        private ParserConfig _ParserConfig = new();
    }
}
