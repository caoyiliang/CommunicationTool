using CommunicationTool.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Config;
using Config.Model;
using Parser;
using Parser.Interfaces;
using Parser.Parsers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using TopPortLib;
using TopPortLib.Interfaces;
using Utils;

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
        private SerialPortTest _test;
        [ObservableProperty]
        private SerialPortConnection _serialPortConnection;
        [ObservableProperty]
        private ParserConfig _parserConfig;
        [ObservableProperty]
        private string? _Title;

        [ObservableProperty]
        private ReceiveViewModel _receiveViewModel = new();
        [ObservableProperty]
        private SendViewModel _sendViewModel = new();

        [ObservableProperty]
        private ObservableCollection<SendCmd> _sendCmds;
        [ObservableProperty]
        private IEnumerable<DataType> _sendTypes;
        [ObservableProperty]
        private IEnumerable<CrcType> _CrcTypes;

        private readonly Connection _config;
#pragma warning disable CA1859 // 尽可能使用具体类型以提高性能W
        private ITopPort? _SerialPort;
#pragma warning restore CA1859 // 尽可能使用具体类型以提高性能
        public SerialPortViewModel(Connection config, SerialPortTest test)
        {
            SendTypes = Enum.GetValues<DataType>();
            CrcTypes = Enum.GetValues<CrcType>();
            PortNames = SerialPort.GetPortNames();
            StopBits = Enum.GetValues<StopBits>();
            Parity = Enum.GetValues<Parity>();
            _config = config;
            Test = test;
            Title = test.TestName;
            SerialPortConnection = test.SerialPortConnection;
            SerialPortConnection.PropertyChanged += SerialPortConnection_PropertyChanged; ;
            ParserConfig = test.ParserConfig;
            ParserConfig.PropertyChanged += ParserConfig_PropertyChanged;
            SendCmds = test.SendCmds;
            SendCmds.CollectionChanged += SendCmds_CollectionChanged;
            foreach (var item in SendCmds)
            {
                item.PropertyChanged += SendCmd_PropertyChanged;
            }
            Status = SerialPortConnection.ToString();
        }

        private async void SendCmds_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                    foreach (SendCmd item in e.NewItems)
                    {
                        item.PropertyChanged += SendCmd_PropertyChanged;
                        item.Id = Guid.NewGuid();
                    }
            }
            else
            {
                await _config.TrySaveChangeAsync();
            }
        }

        private async void SendCmd_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            await _config.TrySaveChangeAsync();
        }

        partial void OnIsConnectChanged(bool value)
        {
            SendViewModel.CommunicationPort = _SerialPort;
            SendViewModel.IsConnect = value;
        }

        partial void OnTitleChanged(string? value)
        {
            Test.TestName = value;
            _ = Task.Run(async () => await _config.TrySaveChangeAsync());
        }

        private async void SerialPortConnection_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            await _config.TrySaveChangeAsync();
            Status = SerialPortConnection.ToString();
        }

        private async void ParserConfig_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            await _config.TrySaveChangeAsync();
        }

        [RelayCommand]
        private async Task SetSerialPortConfigAsync()
        {
            if (!IsConnect)
            {
                PortNames = SerialPort.GetPortNames();
                IsPopupVisible = true;
            }
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
            if (_SerialPort != null)
            {
                await _SerialPort.CloseAsync().ConfigureAwait(false);
                _SerialPort.Dispose();
                _SerialPort = null;
            }
            _config.SerialPortTests.Remove(Test);
            await _config.TrySaveChangeAsync().ConfigureAwait(false);
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
                var serialPort = new Communication.Bus.PhysicalPort.SerialPort(SerialPortConnection.PortName, SerialPortConnection.BaudRate, SerialPortConnection.Parity, SerialPortConnection.DataBits, SerialPortConnection.StopBits)
                {
                    DtrEnable = SerialPortConnection.DTR,
                    RtsEnable = SerialPortConnection.RTS
                };
                _SerialPort = new TopPort(serialPort, NewParser());

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

        private IParser NewParser()
        {
            switch (ParserConfig.ParserType)
            {
                case ParserType.TimeParser:
                    return new TimeParser(ParserConfig.Time);
                case ParserType.HeadLengthParser:
                    {
                        var Head = ParserConfig.Head;
                        if (string.IsNullOrEmpty(Head))
                            return new HeadLengthParser(GetDataLength);
                        else
                            return new HeadLengthParser(StringByteUtils.StringToBytes(Head), GetDataLength);
                    }
                case ParserType.HeadFootParser:
                    {
                        if (string.IsNullOrEmpty(ParserConfig.Head) || string.IsNullOrEmpty(ParserConfig.Foot))
                            throw new Exception("Head or Foot is null");
                        return new HeadFootParser(StringByteUtils.StringToBytes(ParserConfig.Head), StringByteUtils.StringToBytes(ParserConfig.Foot));
                    }
                case ParserType.FootParser:
                    {
                        if (string.IsNullOrEmpty(ParserConfig.Foot))
                            throw new Exception("Foot is null");
                        return new FootParser(StringByteUtils.StringToBytes(ParserConfig.Foot));
                    }
                default:
                    return new NoParser();
            }
        }

        private async Task<GetDataLengthRsp> GetDataLength(byte[] data)
        {
            var headLenth = string.IsNullOrEmpty(ParserConfig.Head) ? 0 : StringByteUtils.StringToBytes(ParserConfig.Head).Length;
            int Length = 0;
            switch (ParserConfig.DataType)
            {
                case Config.LengthDataType.Float:
                    if (data.Length < headLenth + 4) return new GetDataLengthRsp() { StateCode = Parser.StateCode.LengthNotEnough };
                    Length = (int)StringByteUtils.ToSingle(data, headLenth, ParserConfig.IsHighByteBefore);
                    break;
                case Config.LengthDataType.Int16:
                    if (data.Length < headLenth + 2) return new GetDataLengthRsp() { StateCode = Parser.StateCode.LengthNotEnough };
                    Length = StringByteUtils.ToInt16(data, headLenth, ParserConfig.IsHighByteBefore);
                    break;
                case Config.LengthDataType.UInt16:
                    if (data.Length < headLenth + 2) return new GetDataLengthRsp() { StateCode = Parser.StateCode.LengthNotEnough };
                    Length = StringByteUtils.ToUInt16(data, headLenth, ParserConfig.IsHighByteBefore);
                    break;
                case Config.LengthDataType.Int32:
                    if (data.Length < headLenth + 4) return new GetDataLengthRsp() { StateCode = Parser.StateCode.LengthNotEnough };
                    Length = StringByteUtils.ToInt32(data, headLenth, ParserConfig.IsHighByteBefore);
                    break;
                case Config.LengthDataType.固定长度:
                    Length = ParserConfig.Length;
                    break;
                default:
                    break;
            }
            return await Task.FromResult(new GetDataLengthRsp()
            {
                Length = Length,
                StateCode = Parser.StateCode.Success
            });
        }

        private bool CanOpen()
        {
            return !IsOpen || IsConnect;
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

        private async Task SerialPort_OnReceiveParsedData(byte[] data)
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                ReceiveViewModel.CommunicationDatas.Add(new CommunicationData(data, ReceiveViewModel.SelectedShowType, TransferDirection.Response));
                ReceiveViewModel.RsponseLength += data.Length;
            });
        }

        private async Task SerialPort_OnSentData(byte[] data)
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                ReceiveViewModel.CommunicationDatas.Add(new CommunicationData(data, ReceiveViewModel.SelectedShowType, TransferDirection.Request));
                ReceiveViewModel.RequestLength += data.Length;
            });
        }
    }
}