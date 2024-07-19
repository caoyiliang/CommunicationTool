using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Config.Model;
using System.IO.Ports;

namespace CommunicationTool.ViewModel
{
    public partial class SerialPortViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isPopupVisible;
        [ObservableProperty]
        private string[]? _portNames;
        [ObservableProperty]
        private int[] _baudRates = [300, 600, 1200, 2400, 4800, 9600, 19200, 38400, 43000, 57000, 57600, 115200];
        [ObservableProperty]
        private int[] _dataBits = [8, 7, 6];
        [ObservableProperty]
        private IEnumerable<StopBits> _stopBits;
        [ObservableProperty]
        private IEnumerable<Parity> _parity;
        [ObservableProperty]
        private string? _status = "未连接";
        [ObservableProperty]
        SerialPortConnection _connection;

        private readonly Connection _config;
        public SerialPortViewModel(Connection config, SerialPortConnection connection)
        {
            PortNames = SerialPort.GetPortNames();
            StopBits = Enum.GetValues(typeof(StopBits)).Cast<StopBits>();
            Parity = Enum.GetValues(typeof(Parity)).Cast<Parity>();
            _config = config;
            Connection = connection;
        }

        [RelayCommand]
        private async Task SetSerialPortConfigAsync()
        {
            IsPopupVisible = true;
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task CloseAsync()
        {
            _config.SerialPortConnections.Remove(Connection);
            await _config.TrySaveChangeAsync();
        }
    }
}