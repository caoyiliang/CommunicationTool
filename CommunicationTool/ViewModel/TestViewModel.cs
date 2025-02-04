﻿using Communication.Bluetooth;
using CommunicationTool.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Config;
using Config.Interface;
using Config.Model;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Parser;
using Parser.Interfaces;
using Parser.Parsers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using TopPortLib;
using TopPortLib.Interfaces;
using Utils;

namespace CommunicationTool.ViewModel
{
    public partial class TestViewModel : ObservableObject
    {
        private Guid _ClientId = Guid.NewGuid();
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
        private List<string> _hostName = ["Any"];
        [ObservableProperty]
        private BluetoothDevice? _SelectedDevice;
        [ObservableProperty]
        private ObservableCollection<BluetoothDevice> _devices = [];

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
        [ObservableProperty]
        private Guid? _CurrentClientId;

        private readonly Connection _config;
        private CancellationTokenSource? _cts;
        private ITopPort? _TopPort;
        private ITopPort_Server? _TopPort_Server;

        public ObservableCollection<TabItemViewModel> TabItems { get; set; } = [];

        public TestViewModel(Connection config, TestConfig test)
        {
            PortNames = SerialPort.GetPortNames();
            StopBits = Enum.GetValues<StopBits>();
            Parity = Enum.GetValues<Parity>();
            IPHostEntry ipHostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in ipHostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    HostName.Add(ip.ToString());
                }
            }
            BluetoothRadio.Default.Mode = RadioMode.Discoverable;
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
                if (_ClientId == m.Value.parentId)
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var tabItem = TabItems.FirstOrDefault(_ => _.ClientId == m.Value.clientId);
                        if (tabItem != null) TabItems.Remove(tabItem);
                    });
                }
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

        partial void OnSelectedTabItemChanged(TabItemViewModel? value)
        {
            if (value == null) return;
            CurrentClientId = value.ClientId;
            switch (PhysicalPortConnection.Type)
            {
                case TestType.SerialPort:
                case TestType.TcpClient:
                case TestType.UdpClient:
                case TestType.ClassicBluetoothClient:
                    SendViewModel.TestTopPort = _TopPort;
                    break;
                case TestType.TcpServer:
                case TestType.ClassicBluetoothServer:
                    SendViewModel.ClientId = CurrentClientId.Value;
                    SendViewModel.TestTopPort_Server = _TopPort_Server;
                    break;
                default:
                    break;
            }
            SendViewModel.IsConnect = value.IsConnect;
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
                    case TestType.UdpClient:
                        await _TopPort!.CloseAsync();
                        var tabItem = TabItems.Where(_ => _.ClientId == CurrentClientId).First();
                        var str = tabItem.Header;
                        if (str != null)
                            if (str.Contains("掉线尝试重连"))
                            {
                                tabItem.Header = str.Replace(" 掉线尝试重连", " 测试关闭");
                            }
                            else if (!str.Contains("测试关闭"))
                            {
                                tabItem.Header += " 测试关闭";
                            }
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
                            CurrentClientId = Guid.NewGuid();
                            _TopPort = new TopPort(physicalPort, NewParser());

                            _TopPort.OnSentData += async data => await Test_OnSentData(data, CurrentClientId.Value);
                            _TopPort.OnReceiveParsedData += async data => await Test_OnReceiveParsedData(CurrentClientId.Value, data);
                            _TopPort.OnConnect += async () => await Test_OnClientConnect(CurrentClientId.Value);
                            _TopPort.OnDisconnect += TopPort_OnDisconnect;
                        }
                        break;
                    case TestType.TcpClient:
                        {
                            var Connection = (TcpClientConfig)PhysicalPortConnection;
                            var physicalPort = new Communication.Bus.PhysicalPort.TcpClient(Connection.HostName, Connection.Port);
                            CurrentClientId = Guid.NewGuid();
                            _TopPort = new TopPort(physicalPort, NewParser());

                            _TopPort.OnSentData += async data => await Test_OnSentData(data, CurrentClientId.Value);
                            _TopPort.OnReceiveParsedData += async data => await Test_OnReceiveParsedData(CurrentClientId.Value, data);
                            _TopPort.OnConnect += async () => await Test_OnClientConnect(CurrentClientId.Value);
                            _TopPort.OnDisconnect += TopPort_OnDisconnect;
                        }
                        break;
                    case TestType.TcpServer:
                        {
                            var Connection = (TcpServerConfig)PhysicalPortConnection;
                            var physicalPort = new Communication.Bus.TcpServer(Connection.HostName == "Any" ? IPAddress.Any.ToString() : Connection.HostName, Connection.Port);
                            _TopPort_Server = new TopPort_Server(physicalPort, async () => await Task.FromResult(NewParser()));

                            _TopPort_Server.OnClientConnect += Test_OnClientConnect;
                            _TopPort_Server.OnClientDisconnect += TopPort_Server_OnClientDisconnect;
                            _TopPort_Server.OnReceiveParsedData += Test_OnReceiveParsedData;
                            _TopPort_Server.OnSentData += Test_OnSentData;
                        }
                        break;
                    case TestType.UdpClient:
                        {
                            var Connection = (UdpClientConfig)PhysicalPortConnection;
                            var physicalPort = new Communication.Bus.PhysicalPort.UdpClient(Connection.RemoteHostName, Connection.RemotePort, new IPEndPoint(IPAddress.Parse(Connection.HostName), Connection.Port));
                            CurrentClientId = Guid.NewGuid();
                            _TopPort = new TopPort(physicalPort, NewParser());

                            _TopPort.OnSentData += async data => await Test_OnSentData(data, CurrentClientId.Value);
                            _TopPort.OnReceiveParsedData += async data => await Test_OnReceiveParsedData(CurrentClientId.Value, data);
                            _TopPort.OnConnect += async () => await Test_OnClientConnect(CurrentClientId.Value);
                            _TopPort.OnDisconnect += TopPort_OnDisconnect;
                        }
                        break;
                    case TestType.ClassicBluetoothClient:
                        {
                            if (SelectedDevice != null)
                            {
                                var physicalPort = new BluetoothClassic(SelectedDevice.Addr);
                                CurrentClientId = Guid.NewGuid();
                                _TopPort = new TopPort(physicalPort, NewParser());
                                _TopPort.OnSentData += async data => await Test_OnSentData(data, CurrentClientId.Value);
                                _TopPort.OnReceiveParsedData += async data => await Test_OnReceiveParsedData(CurrentClientId.Value, data);
                                _TopPort.OnConnect += async () => await Test_OnClientConnect(CurrentClientId.Value);
                                _TopPort.OnDisconnect += TopPort_OnDisconnect;
                            }
                        }
                        break;
                    case TestType.ClassicBluetoothServer:
                        {
                            var physicalPort = new BluetoothClassic_Server();
                            _TopPort_Server = new TopPort_Server(physicalPort, async () => await Task.FromResult(NewParser()));

                            _TopPort_Server.OnClientConnect += Test_OnClientConnect;
                            _TopPort_Server.OnClientDisconnect += TopPort_Server_OnClientDisconnect;
                            _TopPort_Server.OnReceiveParsedData += Test_OnReceiveParsedData;
                            _TopPort_Server.OnSentData += Test_OnSentData;
                        }
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
            await App.Current.Dispatcher.InvokeAsync(async () =>
            {
                var TabItem = TabItems.SingleOrDefault(_ => _.ClientId == clientId) ?? new TabItemViewModel(_ClientId, clientId) { Header = PhysicalPortConnection.Type == TestType.TcpServer ? await _TopPort_Server!.GetClientInfos(clientId) : PhysicalPortConnection.Info };
                TabItem.IsConnect = true;
                TabItems.Add(TabItem);
                SelectedTabItem = TabItem;
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
                var tabItem = TabItems.First(_ => _.ClientId == CurrentClientId);
                tabItem.IsConnect = false;
                ConnectCommand.NotifyCanExecuteChanged();
                if (!tabItem.Header!.Contains("测试关闭"))
                    tabItem.Header += " 掉线尝试重连";
                //Exception = "通讯断连,等待连接...";
            });
        }

        private async Task Test_OnReceiveParsedData(Guid clientId, byte[] data)
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

        private async Task Test_OnSentData(byte[] data, Guid clientId)
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
            if (IsConnect)
            {
                switch (PhysicalPortConnection.Type)
                {
                    case TestType.SerialPort:
                    case TestType.TcpClient:
                    case TestType.UdpClient:
                    case TestType.ClassicBluetoothClient:
                        await _TopPort!.SendAsync(cmd);
                        break;
                    case TestType.TcpServer:
                    case TestType.ClassicBluetoothServer:
                        await _TopPort_Server!.SendAsync(CurrentClientId!.Value, cmd);
                        break;
                    default:
                        break;
                }
            }
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

        [RelayCommand]
        private async Task ScanAsync()
        {
            Devices.Clear();
            _ = Task.Run(async () =>
            {
                using var bluetoothClient = new BluetoothClient();
                await foreach (var item in bluetoothClient.DiscoverDevicesAsync())
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Devices.Add(new() { Name = item.DeviceName, Addr = item.DeviceAddress.ToString() });
                    });
                }
            });
            await Task.CompletedTask;
        }
    }

    internal class CloseTabItemMessage(Guid parentId, Guid clientId) : ValueChangedMessage<(Guid parentId, Guid clientId)>((parentId, clientId)) { }
}