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
using System.IO;
using System.IO.Ports;
using System.Management;
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
        private readonly Guid _ClientId = Guid.NewGuid();
        [ObservableProperty]
        private bool _isPopupVisible;
        [ObservableProperty]
        private bool _hasPopupVisible;
        [ObservableProperty]
        private List<string> _portNames = [];
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
        private ITopPort_M2M? _TopPort_M2M;
        private PushQueue<(Guid clientId, DateTime dateTime, byte[] data, TransferDirection transferDirection)>? _receiveQueue;

        public ObservableCollection<TabItemViewModel> TabItems { get; set; } = [];

        public TestViewModel(Connection config, TestConfig test)
        {
            RefreshPortNames();
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

        private void RefreshPortNames()
        {
            PortNames.Clear();
            // 用于存储端口号和设备名的映射
            var portNames = SerialPort.GetPortNames();
            var portDescriptions = new Dictionary<string, string>();

            // 使用WMI获取设备描述
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
            {
                foreach (var obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString();
                    if (name != null)
                    {
                        // 匹配所有 (COMx)
                        var matches = System.Text.RegularExpressions.Regex.Matches(name, @"COM\d+");
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            var port = match.Value; // 得到 COM1、COM2
                            portDescriptions[port] = name;
                        }
                    }
                }
            }

            // 组合显示
            foreach (var port in portNames)
            {
                if (portDescriptions.TryGetValue(port, out var desc))
                {
                    // 去掉括号及其内容，只保留设备名
                    var deviceName = System.Text.RegularExpressions.Regex.Replace(desc, @"\s*\(.*?\)", "").Trim();
                    PortNames.Add($"{port} - {deviceName}");
                }
                else
                {
                    PortNames.Add(port);
                }
            }
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

        //byte[] bytes = (byte[])Array.CreateInstance(typeof(byte), 1009);
        partial void OnIsAutoSendChanged(bool value)
        {
            Test.IsAutoSend = value;
            _ = Task.Run(async () => await _config.TrySaveChangeAsync());
            if (value)
            {
                _cts = new();
                _ = Task.Run(async () =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        foreach (var item in SendCmds)
                        {
                            if (item.IsSelected)
                            {
                                if (CurrentSendId != item.Id)
                                {
                                    if (SendInterval >= 200)
                                        await App.Current.Dispatcher.InvokeAsync(() => CurrentSendId = item.Id);
                                }

                                await Task.Run(async () =>
                                {
                                    try
                                    {
                                        await SendDataAsync(item);
                                    }
                                    catch (DriveNotFoundException) { }
                                    if (SendInterval > 0)
                                    {
                                        try
                                        {
                                            await Task.Delay(SendInterval, _cts.Token);
                                        }
                                        catch (TaskCanceledException) { }
                                    }
                                });
                            }
                        }
                    }
                    await App.Current.Dispatcher.InvokeAsync(() => CurrentSendId = Guid.Empty);
                });
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
                case TestType.ClassicBluetoothClient:
                    SendViewModel.TestTopPort = _TopPort;
                    break;
                case TestType.TcpServer:
                case TestType.ClassicBluetoothServer:
                    SendViewModel.ClientId = CurrentClientId.Value;
                    SendViewModel.TestTopPort_Server = _TopPort_Server;
                    break;
                case TestType.UdpClient:
                    SendViewModel.ClientId = CurrentClientId.Value;
                    SendViewModel.TestTopPort_M2M = _TopPort_M2M;
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
                RefreshPortNames();
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
                        {
                            await _TopPort_Server!.CloseAsync();
                            IsConnect = false;
                        }
                        break;
                    case TestType.UdpClient:
                        {
                            await _TopPort_M2M!.CloseAsync();
                            foreach (var item in TabItems)
                            {
                                var str = item.Header;
                                item.IsConnect = false;
                                if (str != null)
                                    if (!str.Contains("测试关闭"))
                                    {
                                        item.Header += " 测试关闭";
                                    }
                            }
                            IsConnect = false;
                        }
                        break;
                    case TestType.SerialPort:
                    case TestType.TcpClient:
                        {
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
                            var physicalPort = new Communication.Bus.PhysicalPort.SerialPort(Connection.PortName.Split(" - ")[0], Connection.BaudRate, Connection.Parity, Connection.DataBits, Connection.StopBits)
                            {
                                DtrEnable = Connection.DTR,
                                RtsEnable = Connection.RTS
                            };
                            var currentClientId = Guid.NewGuid();
                            _TopPort = new TopPort(physicalPort, NewParser());

                            _TopPort.OnSentData += async data => await Test_OnSentData(data, currentClientId);
                            _TopPort.OnReceiveParsedData += async data => await Test_OnReceiveParsedData(currentClientId, data);
                            _TopPort.OnConnect += async () => await Test_OnClientConnect(currentClientId);
                            _TopPort.OnDisconnect += TopPort_OnDisconnect;
                        }
                        break;
                    case TestType.TcpClient:
                        {
                            var Connection = (TcpClientConfig)PhysicalPortConnection;
                            var physicalPort = new Communication.Bus.PhysicalPort.TcpClient(Connection.HostName, Connection.Port);
                            var currentClientId = Guid.NewGuid();
                            _TopPort = new TopPort(physicalPort, NewParser());

                            _TopPort.OnSentData += async data => await Test_OnSentData(data, currentClientId);
                            _TopPort.OnReceiveParsedData += async data => await Test_OnReceiveParsedData(currentClientId, data);
                            _TopPort.OnConnect += async () => await Test_OnClientConnect(currentClientId);
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
                            var physicalPort = new Communication.Bus.Udp(Connection.HostName, Connection.Port);
                            _TopPort_M2M = new TopPort_M2M(physicalPort, async () => await Task.FromResult(NewParser()));

                            _TopPort_M2M.OnClientConnect += Test_OnClientConnect;
                            _TopPort_M2M.OnReceiveParsedData += Test_OnReceiveParsedData;
                            _TopPort_M2M.OnSentData += Test_OnSentData;

                            SendViewModel.HostName = Connection.RemoteHostName;
                            SendViewModel.Port = Connection.RemotePort;
                            SendViewModel.TestTopPort_M2M = _TopPort_M2M;
                        }
                        break;
                    case TestType.ClassicBluetoothClient:
                        {
                            if (SelectedDevice != null)
                            {
                                var physicalPort = new BluetoothClassic(SelectedDevice.Addr);
                                var currentClientId = Guid.NewGuid();
                                _TopPort = new TopPort(physicalPort, NewParser());
                                _TopPort.OnSentData += async data => await Test_OnSentData(data, currentClientId);
                                _TopPort.OnReceiveParsedData += async data => await Test_OnReceiveParsedData(currentClientId, data);
                                _TopPort.OnConnect += async () => await Test_OnClientConnect(currentClientId);
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

                _receiveQueue = new(false) { MaxCacheCount = int.MaxValue };
                _receiveQueue.OnPushData += ReceiveQueue_OnPushData;
                await _receiveQueue.StartAsync();

                try
                {
                    if (_TopPort != null) await _TopPort.OpenAsync();
                    if (_TopPort_Server != null) { await _TopPort_Server.OpenAsync(); IsConnect = true; }
                    if (_TopPort_M2M != null) { await _TopPort_M2M.OpenAsync(); IsConnect = true; SendViewModel.IsConnect = true; }
                    IsOpen = true;
                }
                catch
                {
                    //ExceptionStr = "连接失败，检查链路";
                }
            }
        }

        private async Task ReceiveQueue_OnPushData((Guid clientId, DateTime dateTime, byte[] data, TransferDirection transferDirection) arg)
        {
            try
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    var receiveViewModel = TabItems.FirstOrDefault(_ => _.ClientId == arg.clientId)?.ReceiveViewModel;
                    if (receiveViewModel != null)
                    {
                        receiveViewModel.CommunicationDatas.Add(new CommunicationData(arg.data, receiveViewModel.SelectedShowType, arg.transferDirection) { DateTime = arg.dateTime });
                        switch (arg.transferDirection)
                        {
                            case TransferDirection.Request:
                                receiveViewModel.RequestLength += arg.data.Length;
                                break;
                            case TransferDirection.Response:
                                receiveViewModel.RsponseLength += arg.data.Length;
                                break;
                            default:
                                break;
                        }
                    }
                });
                await Task.Delay(1);
            }
            catch { }
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
                            return new HeadLengthParser(GetDataLength, false);
                        else
                            return new HeadLengthParser(StringByteUtils.StringToBytes(Head), GetDataLength, false);
                    }
                case ParserType.HeadFootParser:
                    {
                        if (string.IsNullOrEmpty(ParserConfig.Head) || string.IsNullOrEmpty(ParserConfig.Foot))
                            throw new Exception("Head or Foot is null");
                        return new HeadFootParser(StringByteUtils.StringToBytes(ParserConfig.Head), StringByteUtils.StringToBytes(ParserConfig.Foot), false);
                    }
                case ParserType.FootParser:
                    {
                        if (string.IsNullOrEmpty(ParserConfig.Foot))
                            throw new Exception("Foot is null");
                        return new FootParser(StringByteUtils.StringToBytes(ParserConfig.Foot), false);
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
            if (PhysicalPortConnection.Type == TestType.TcpServer || PhysicalPortConnection.Type == TestType.ClassicBluetoothServer || PhysicalPortConnection.Type == TestType.UdpClient) return true;
            return !IsOpen || IsConnect;
        }

        private async Task Test_OnClientConnect(Guid clientId)
        {
            await App.Current.Dispatcher.InvokeAsync(async () =>
            {
                var TabItem = TabItems.SingleOrDefault(_ => _.ClientId == clientId) ?? new TabItemViewModel(_ClientId, clientId) { Header = (PhysicalPortConnection.Type == TestType.TcpServer || PhysicalPortConnection.Type == TestType.UdpClient) ? (PhysicalPortConnection.Type == TestType.UdpClient ? await _TopPort_M2M!.GetClientInfos(clientId) : await _TopPort_Server!.GetClientInfos(clientId)) : PhysicalPortConnection.Info };
                TabItem.IsConnect = true;
                TabItem.Header = TabItem.Header?.Replace(" 掉线尝试重连", string.Empty);
                if (!TabItems.Contains(TabItem)) TabItems.Add(TabItem);
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
                await _receiveQueue!.PutInDataAsync((clientId, DateTime.Now, data, TransferDirection.Response));
            }
            catch { }
        }

        private async Task Test_OnSentData(byte[] data, Guid clientId)
        {
            try
            {
                await _receiveQueue!.PutInDataAsync((clientId, DateTime.Now, data, TransferDirection.Request));
            }
            catch { }
        }

        [RelayCommand(CanExecute = nameof(CanSend))]
        private async Task SendDataAsync(object send)
        {
            if (send is not SendCmd sendCmd) return;
            var cmd = sendCmd.SendCmds;
            if (cmd.Length == 0) return;

            if (IsConnect)
            {
                switch (PhysicalPortConnection.Type)
                {
                    case TestType.SerialPort:
                    case TestType.TcpClient:
                    case TestType.ClassicBluetoothClient:
                        await _TopPort!.SendAsync(cmd);
                        break;
                    case TestType.TcpServer:
                    case TestType.ClassicBluetoothServer:
                        if (CurrentClientId != null) await _TopPort_Server!.SendAsync(CurrentClientId.Value, cmd);
                        break;
                    case TestType.UdpClient:
                        var Connection = (UdpClientConfig)PhysicalPortConnection;
                        if (_TopPort_M2M != null) await _TopPort_M2M.SendAsync(Connection.RemoteHostName, Connection.RemotePort, cmd);
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