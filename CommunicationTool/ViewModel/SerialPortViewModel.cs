using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Config.Model;
using System.IO.Ports;
using TopPortLib;
using TopPortLib.Interfaces;

namespace CommunicationTool.ViewModel
{
    public partial class SerialPortViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isPopupVisible;
        [ObservableProperty]
        private bool _hasPopupVisible;
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
        private bool _isConnect;
        [ObservableProperty]
        private bool _isOpen;
        [ObservableProperty]
        private SerialPortConnection _connection;
        [ObservableProperty]
        private ParserConfig _parserConfig;

        private readonly Connection _config;
        private ITopPort? _SerialPort;
        public SerialPortViewModel(Connection config, SerialPortConnection connection)
        {
            PortNames = SerialPort.GetPortNames();
            StopBits = Enum.GetValues<StopBits>();
            Parity = Enum.GetValues<Parity>();
            _config = config;
            Connection = connection;
            ParserConfig = connection.ParserConfig;
            Connection.PropertyChanged += Connection_PropertyChanged;
            Status = Connection.ToString();
        }

        private async void Connection_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            await _config.TrySaveChangeAsync();
            Status = Connection.ToString();
        }

        [RelayCommand]
        private async Task SetSerialPortConfigAsync()
        {
            if (!IsConnect) IsPopupVisible = true;
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task HasPopupAsync()
        {
            HasPopupVisible = !HasPopupVisible;
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task CloseAsync()
        {
            _config.SerialPortConnections.Remove(Connection);
            await _config.TrySaveChangeAsync();
        }

        [RelayCommand(CanExecute = nameof(CanOpen))]
        private async Task ConnectAsync()
        {
            if (IsConnect)
            {
                await _SerialPort!.CloseAsync();
                IsOpen = false;
            }
            else
            {
                var serialPort = new Communication.Bus.PhysicalPort.SerialPort(Connection.PortName, Connection.BaudRate, Connection.Parity, Connection.DataBits, Connection.StopBits)
                {
                    DtrEnable = Connection.DTR,
                    RtsEnable = Connection.RTS
                };
                _SerialPort = new TopPort(serialPort, new Parser.Parsers.TimeParser());

                _SerialPort.OnSentData += SerialPort_OnSentData;
                _SerialPort.OnReceiveParsedData += SerialPort_OnReceiveParsedData;
                _SerialPort.OnConnect += SerialPort_OnConnect;
                _SerialPort.OnDisconnect += SerialPort_OnDisconnect;
                try
                {
                    await _SerialPort.OpenAsync();
                    IsOpen = true;
                }
                catch
                {
                    //ExceptionStr = "连接失败，检查链路";
                }
            }
        }

        private bool CanOpen()
        {
            if (IsOpen && !IsConnect)
            {
                return false;
            }
            return true;
        }

        private async Task SerialPort_OnDisconnect()
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                IsConnect = false;
                ConnectCommand.NotifyCanExecuteChanged();
                //Exception = "通讯断连,等待连接...";
            });
        }

        private async Task SerialPort_OnConnect()
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                IsConnect = true;
                ConnectCommand.NotifyCanExecuteChanged();
                //Exception = "";
            });
        }

        private Task SerialPort_OnReceiveParsedData(byte[] data)
        {
            throw new NotImplementedException();
        }

        private Task SerialPort_OnSentData(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}