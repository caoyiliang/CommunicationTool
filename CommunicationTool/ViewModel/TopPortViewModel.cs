﻿using Communication.Interfaces;
using CommunicationTool.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public partial class TopPortViewModel : ObservableObject
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
        private ReceiveViewModel _receiveViewModel = new();
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

        private readonly Connection _config;
        private CancellationTokenSource? _cts;
        private ITopPort? _TopPort;

        public TopPortViewModel(Connection config, TestConfig test)
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
            SendViewModel.CommunicationPort = _TopPort;
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
                await _TopPort!.CloseAsync();
                IsOpen = false;
            }
            else
            {
                IPhysicalPort physicalPort = null;
                switch (PhysicalPortConnection.Type)
                {
                    case TestType.SerialPort:
                        {
                            var Connection = (SerialPortConfig)PhysicalPortConnection;
                            physicalPort = new Communication.Bus.PhysicalPort.SerialPort(Connection.PortName, Connection.BaudRate, Connection.Parity, Connection.DataBits, Connection.StopBits)
                            {
                                DtrEnable = Connection.DTR,
                                RtsEnable = Connection.RTS
                            };
                        }
                        break;
                    case TestType.TcpClient:
                        {
                            var Connection = (TcpClientConfig)PhysicalPortConnection;
                            physicalPort = new Communication.Bus.PhysicalPort.TcpClient(Connection.HostName, Connection.Port);
                        }
                        break;
                    case TestType.TcpServer:
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

                _TopPort = new TopPort(physicalPort, NewParser());

                _TopPort.OnSentData += SerialPort_OnSentData;
                _TopPort.OnReceiveParsedData += SerialPort_OnReceiveParsedData;
                _TopPort.OnConnect += SerialPort_OnConnect;
                _TopPort.OnDisconnect += SerialPort_OnDisconnect;
                try
                {
                    await _TopPort.OpenAsync();
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
            try
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    ReceiveViewModel.CommunicationDatas.Add(new CommunicationData(data, ReceiveViewModel.SelectedShowType, TransferDirection.Response));
                    ReceiveViewModel.RsponseLength += data.Length;
                });
            }
            catch { }
        }

        private async Task SerialPort_OnSentData(byte[] data)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ReceiveViewModel.CommunicationDatas.Add(new CommunicationData(data, ReceiveViewModel.SelectedShowType, TransferDirection.Request));
                        ReceiveViewModel.RequestLength += data.Length;
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
}