using CommunicationTool.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Config;
using Config.Interface;
using Config.Model;
using Parser;
using Parser.Interfaces;
using Parser.Parsers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using System.Windows;
using TopPortLib;
using TopPortLib.Interfaces;
using Utils;

namespace CommunicationTool.ViewModel
{
    public partial class TestViewModel : ObservableObject
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
        [NotifyCanExecuteChangedFor(nameof(SendDataCommand))]
        private bool _isConnect;
        [ObservableProperty]
        private bool _isOpen;
        [ObservableProperty]
        private TestConfig _test;
        [ObservableProperty]
        private IPhysicalPortConfig _physicalPortConnection;
        [ObservableProperty]
        private ParserConfig _parserConfig;
        [ObservableProperty]
        private string? _Title;

        [ObservableProperty]
        private SendViewModel _sendViewModel = new();

        [ObservableProperty]
        private ObservableCollection<SendCmd> _sendCmds;
        [ObservableProperty]
        private bool _isAutoSend;
        [ObservableProperty]
        private int _sendInterval;
        [ObservableProperty]
        private Guid _currentSendId = Guid.Empty;
        [ObservableProperty]
        private TabItemViewModel? _SelectedTabItem;

        private readonly Connection _config;
        private CancellationTokenSource? _cts;
        private ITopPort? _TopPort;
        private ITopPort_Server? _TopPort_Server;
        private TabItemViewModel? _tabItem;
        public ObservableCollection<TabItemViewModel> TabItems { get; set; } = [];

        public TestViewModel(Connection config, TestConfig test)
        {
            PortNames = SerialPort.GetPortNames();
            StopBits = Enum.GetValues<StopBits>();
            Parity = Enum.GetValues<Parity>();
            _config = config;
            Test = test;
            Title = test.TestName;
            PhysicalPortConnection = test.PhysicalPortConnection;
            PhysicalPortConnection.PropertyChanged += PhysicalPortConnection_PropertyChanged;
            ParserConfig = test.ParserConfig;
            ParserConfig.PropertyChanged += ParserConfig_PropertyChanged;
            IsAutoSend = test.IsAutoSend;
            SendInterval = test.SendInterval;
            SendCmds = test.SendCmds;
            SendCmds.CollectionChanged += SendCmds_CollectionChanged;
            foreach (var item in SendCmds)
            {
                item.PropertyChanged += SendCmd_PropertyChanged;
            }
            Status = PhysicalPortConnection.ToString();
            WeakReferenceMessenger.Default.Register<CloseTabItemMessage>(this, async (r, m) =>
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    TabItems.Remove(TabItems.Where(_ => _.ClientId == m.Value).First());
                });
            });
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
            if (sender is SendCmd sendCmd)
            {
                if (e.PropertyName == nameof(SendCmd.DisplayCmd) || e.PropertyName == nameof(SendCmd.SendType))
                {
                    var value = sendCmd.DisplayCmd;
                    if (!string.IsNullOrEmpty(value))
                    {
                        try
                        {
                            sendCmd.Cmd = sendCmd.SendType switch
                            {
                                DataType.ASCII => Encoding.ASCII.GetBytes(value),
                                DataType.UTF8 => Encoding.UTF8.GetBytes(value),
                                DataType.GB2312 => Encoding.GetEncoding("GB2312").GetBytes(value),
                                _ => StringByteUtils.StringToBytes(value),
                            };
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("数据错误，请检查选择类型！");
                        }
                    }
                    else
                    {
                        sendCmd.Cmd = [];
                    }
                }
            }

            await _config.TrySaveChangeAsync();
        }

        partial void OnIsAutoSendChanged(bool value)
        {
            Test.IsAutoSend = value;
            _ = Task.Run(async () => await _config.TrySaveChangeAsync());
            if (value)
            {
                _cts = new();
                try
                {
                    _ = Task.Run(async () =>
                    {
                        while (!_cts.IsCancellationRequested)
                        {
                            foreach (var item in SendCmds)
                            {
                                if (item.IsSelected)
                                {
                                    if (CurrentSendId != item.Id) CurrentSendId = item.Id; // 设置当前发送行的 Id
                                    await SendDataAsync(item);
                                    try
                                    {
                                        await Task.Delay(SendInterval, _cts.Token);
                                    }
                                    catch { }
                                }
                            }
                            CurrentSendId = Guid.Empty; // 发送完成后重置 Id
                        }
                    }, _cts.Token);
                }
                catch { }
            }
            else
            {
                _cts?.Cancel();
            }
        }

        partial void OnIsConnectChanged(bool value)
        {
            switch (PhysicalPortConnection.Type)
            {
                case TestType.SerialPort:
                case TestType.TcpClient:
                    SendViewModel.TestTopPort = _TopPort;
                    break;
                case TestType.TcpServer:
                    SendViewModel.ClientId = _tabItem!.ClientId;
                    SendViewModel.TestTopPort_Server = _TopPort_Server;
                    break;
                case TestType.UdpClient:
                    break;
                case TestType.ClassicBluetoothClient:
                    break;
                case TestType.ClassicBluetoothServer:
                    break;
                default:
                    break;
            }
            SendViewModel.IsConnect = value;
        }

        partial void OnTitleChanged(string? value)
        {
            Test.TestName = value;
            _ = Task.Run(async () => await _config.TrySaveChangeAsync());
        }

        private async void PhysicalPortConnection_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            await _config.TrySaveChangeAsync();
            Status = PhysicalPortConnection.ToString();
        }

        private async void ParserConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
            if (_TopPort != null)
            {
                await _TopPort.CloseAsync().ConfigureAwait(false);
                _TopPort.Dispose();
                _TopPort = null;
            }
            _config.TestConfigs.Remove(Test);
            await _config.TrySaveChangeAsync().ConfigureAwait(false);
        }

        [RelayCommand(CanExecute = nameof(CanOpen))]
        private async Task ConnectAsync()
        {
            if (IsConnect)
            {
                switch (PhysicalPortConnection.Type)
                {
                    case TestType.TcpServer:
                        await _TopPort_Server!.CloseAsync();
                        IsConnect = false;
                        break;
                    case TestType.SerialPort:
                    case TestType.TcpClient:
                        await _TopPort!.CloseAsync();
                        var str = _tabItem!.Header;
                        if (str != null)
                            if (str.Contains("掉线尝试重连"))
                            {
                                _tabItem.Header = str.Replace(" 掉线尝试重连", " 测试关闭");
                            }
                            else if (!str.Contains("测试关闭"))
                            {
                                _tabItem.Header += " 测试关闭";
                            }
                        break;

                    case TestType.UdpClient:
                        break;
                    case TestType.ClassicBluetoothClient:
                        break;
                    case TestType.ClassicBluetoothServer:
                        break;
                    default:
                        break;
                }

                IsOpen = false;
            }
            else
            {
                switch (PhysicalPortConnection.Type)
                {
                    case TestType.SerialPort:
                        {
                            var Connection = (SerialPortConfig)PhysicalPortConnection;
                            var physicalPort = new Communication.Bus.PhysicalPort.SerialPort(Connection.PortName, Connection.BaudRate, Connection.Parity, Connection.DataBits, Connection.StopBits)
                            {
                                DtrEnable = Connection.DTR,
                                RtsEnable = Connection.RTS
                            };

                            _TopPort = new TopPort(physicalPort, NewParser());

                            _TopPort.OnSentData += TopPort_OnSentData;
                            _TopPort.OnReceiveParsedData += TopPort_OnReceiveParsedData;
                            _TopPort.OnConnect += async () => await Test_OnClientConnect(Guid.NewGuid());
                            _TopPort.OnDisconnect += TopPort_OnDisconnect;
                        }
                        break;
                    case TestType.TcpClient:
                        {
                            var Connection = (TcpClientConfig)PhysicalPortConnection;
                            var physicalPort = new Communication.Bus.PhysicalPort.TcpClient(Connection.HostName, Connection.Port);

                            _TopPort = new TopPort(physicalPort, NewParser());

                            _TopPort.OnSentData += TopPort_OnSentData;
                            _TopPort.OnReceiveParsedData += TopPort_OnReceiveParsedData;
                            _TopPort.OnConnect += async () => await Test_OnClientConnect(Guid.NewGuid());
                            _TopPort.OnDisconnect += TopPort_OnDisconnect;
                        }
                        break;
                    case TestType.TcpServer:
                        {
                            var Connection = (TcpServerConfig)PhysicalPortConnection;
                            var physicalPort = new Communication.Bus.TcpServer(Connection.HostName, Connection.Port);
                            _TopPort_Server = new TopPort_Server(physicalPort, async () => await Task.FromResult(NewParser()));

                            _TopPort_Server.OnClientConnect += Test_OnClientConnect;
                            _TopPort_Server.OnClientDisconnect += TopPort_Server_OnClientDisconnect;
                            _TopPort_Server.OnReceiveParsedData += TopPort_Server_OnReceiveParsedData;
                            _TopPort_Server.OnSentData += TopPort_Server_OnSentData;
                        }
                        break;
                    case TestType.UdpClient:
                        break;
                    case TestType.ClassicBluetoothClient:
                        break;
                    case TestType.ClassicBluetoothServer:
                        break;
                    default:
                        break;
                }


                try
                {
                    if (_TopPort != null) await _TopPort.OpenAsync();
                    if (_TopPort_Server != null) await _TopPort_Server.OpenAsync();
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

        private async Task Test_OnClientConnect(Guid clientId)
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                _tabItem = TabItems.SingleOrDefault(_ => _.Header == PhysicalPortConnection.Info) ?? new TabItemViewModel(clientId) { Header = PhysicalPortConnection.Info };
                _tabItem.IsConnect = true;
                TabItems.Add(_tabItem);
                SelectedTabItem = _tabItem;
                IsConnect = true;
                ConnectCommand.NotifyCanExecuteChanged();
                //Exception = "";
            });
        }

        private async Task TopPort_Server_OnClientDisconnect(Guid clientId)
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                var tabItem = TabItems.SingleOrDefault(_ => _.ClientId == clientId);
                if (tabItem != null)
                {
                    tabItem.IsConnect = false;
                    ConnectCommand.NotifyCanExecuteChanged();
                    tabItem.Header += " 测试关闭";
                }
                //Exception = "通讯断连,等待连接...";
            });
        }

        private async Task TopPort_OnDisconnect()
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                IsConnect = false;
                _tabItem!.IsConnect = false;
                ConnectCommand.NotifyCanExecuteChanged();
                if (!_tabItem!.Header!.Contains("测试关闭"))
                    _tabItem!.Header += " 掉线尝试重连";
                //Exception = "通讯断连,等待连接...";
            });
        }

        private async Task TopPort_Server_OnReceiveParsedData(Guid clientId, byte[] data)
        {
            try
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    var receiveViewModel = TabItems.SingleOrDefault(_ => _.ClientId == clientId)?.ReceiveViewModel;
                    if (receiveViewModel != null)
                    {
                        receiveViewModel.CommunicationDatas.Add(new CommunicationData(data, receiveViewModel.SelectedShowType, TransferDirection.Response));
                        receiveViewModel.RsponseLength += data.Length;
                    }
                });
            }
            catch { }
        }

        private async Task TopPort_Server_OnSentData(byte[] data, Guid clientId)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var receiveViewModel = TabItems.SingleOrDefault(_ => _.ClientId == clientId)?.ReceiveViewModel;
                        if (receiveViewModel != null)
                        {
                            receiveViewModel.CommunicationDatas.Add(new CommunicationData(data, receiveViewModel.SelectedShowType, TransferDirection.Request));
                            receiveViewModel.RequestLength += data.Length;
                        }
                    });
                }
                catch { }
            });
            await Task.CompletedTask;
        }

        private async Task TopPort_OnReceiveParsedData(byte[] data)
        {
            try
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    var receiveViewModel = _tabItem!.ReceiveViewModel;
                    receiveViewModel.CommunicationDatas.Add(new CommunicationData(data, receiveViewModel.SelectedShowType, TransferDirection.Response));
                    receiveViewModel.RsponseLength += data.Length;
                });
            }
            catch { }
        }

        private async Task TopPort_OnSentData(byte[] data)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var receiveViewModel = _tabItem!.ReceiveViewModel;
                        receiveViewModel.CommunicationDatas.Add(new CommunicationData(data, receiveViewModel.SelectedShowType, TransferDirection.Request));
                        receiveViewModel.RequestLength += data.Length;
                    });
                }
                catch { }
            });
            await Task.CompletedTask;
        }

        [RelayCommand(CanExecute = nameof(CanSend))]
        private async Task SendDataAsync(object send)
        {
            if (send is not SendCmd sendCmd) return;
            var cmd = sendCmd.Cmd;
            if (cmd.Length == 0) return;

            cmd = sendCmd.CrcType switch
            {
                CrcType.Modbus => CRC.Crc16(cmd),
                CrcType.Modbus_R => StringByteUtils.ComibeByteArray(cmd, CRC.CRC16_R(cmd)),
                CrcType.GB => StringByteUtils.ComibeByteArray(cmd, CRC.GBcrc16(cmd, cmd.Length)),
                CrcType.GB_string => StringByteUtils.ComibeByteArray(cmd, Encoding.GetEncoding("gb2312").GetBytes(StringByteUtils.BytesToString(CRC.GBcrc16(cmd, cmd.Length)).Replace(" ", ""))),
                CrcType.GB_protocol => ProcessGBProtocol(cmd),
                CrcType.HB => StringByteUtils.ComibeByteArray(cmd, CRC.HBcrc16(cmd, cmd.Length)),
                CrcType.HB_string => StringByteUtils.ComibeByteArray(cmd, Encoding.GetEncoding("gb2312").GetBytes(StringByteUtils.BytesToString(CRC.HBcrc16(cmd, cmd.Length)).Replace(" ", ""))),
                CrcType.HB_protocol => ProcessHBProtocol(cmd),
                CrcType.UCRC => StringByteUtils.ComibeByteArray(cmd, StringByteUtils.GetBytes(CRC.UpdateCRC(cmd, cmd.Length), true)),
                _ => cmd,
            };
            if (sendCmd.CR) cmd = [.. cmd, 0x0d];
            if (sendCmd.LF) cmd = [.. cmd, 0x0a];
            if (IsConnect) await _TopPort!.SendAsync(cmd);
        }

        private bool CanSend()
        {
            return IsConnect;
        }

        private static byte[] HexStringToByte(string SendData)
        {
            try
            {
                return StringByteUtils.StringToBytes(SendData);
            }
            catch
            {
                MessageBox.Show("请输入正确的Hex值");
                return [];
            }
        }

        private static byte[] ProcessGBProtocol(byte[] cmd)
        {
            var strCmd = Encoding.GetEncoding("gb2312").GetString(cmd);
            return Encoding.GetEncoding("gb2312").GetBytes($"##{strCmd.Length.ToString().PadLeft(4, '0')}{strCmd}{StringByteUtils.BytesToString(CRC.GBcrc16(cmd, cmd.Length)).Replace(" ", "")}");
        }

        private static byte[] ProcessHBProtocol(byte[] cmd)
        {
            var strCmd = Encoding.GetEncoding("gb2312").GetString(cmd);
            return Encoding.GetEncoding("gb2312").GetBytes($"##{strCmd.Length.ToString().PadLeft(4, '0')}{strCmd}{StringByteUtils.BytesToString(CRC.HBcrc16(cmd, cmd.Length)).Replace(" ", "")}");
        }
    }

    internal class CloseTabItemMessage(Guid clientId) : ValueChangedMessage<Guid>(clientId) { }
}