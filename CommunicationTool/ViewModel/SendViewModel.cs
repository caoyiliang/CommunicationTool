using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Config;
using System.Text;
using System.Windows;
using TopPortLib.Interfaces;
using Utils;

namespace CommunicationTool.ViewModel
{
    public partial class SendViewModel : ObservableObject
    {
        [ObservableProperty]
        private IEnumerable<DataType> _sendTypes;
        [ObservableProperty]
        private DataType _SelectedSendType = DataType.HEX;
        [ObservableProperty]
        private bool _CR;
        [ObservableProperty]
        private bool _LF;
        [ObservableProperty]
        private CrcType _CrcType;
        [ObservableProperty]
        private IEnumerable<CrcType> _CrcTypes;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendCommand))]
        private string? _SendData;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendCommand))]
        private bool _isConnect;
        [ObservableProperty]
        private ITopPort? _TestTopPort;
        [ObservableProperty]
        private ITopPort_Server? _TestTopPort_Server;
        [ObservableProperty]
        private ITopPort_M2M? _TestTopPort_M2M;
        [ObservableProperty]
        private Guid _ClientId;
        [ObservableProperty]
        private string? _HostName;
        [ObservableProperty]
        private int _Port;

        public SendViewModel()
        {
            CrcTypes = Enum.GetValues<CrcType>();
            SendTypes = Enum.GetValues<DataType>();
        }

        [RelayCommand(CanExecute = nameof(CanSend))]
        private async Task SendAsync()
        {
            if (string.IsNullOrEmpty(SendData)) return;

            byte[] cmd = SelectedSendType switch
            {
                DataType.ASCII => Encoding.ASCII.GetBytes(SendData),
                DataType.UTF8 => Encoding.UTF8.GetBytes(SendData),
                DataType.GB2312 => Encoding.GetEncoding("GB2312").GetBytes(SendData),
                _ => HexStringToByte(SendData),
            };
            if (cmd.Length == 0) return;
            cmd = CrcType switch
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
            if (CR) cmd = [.. cmd, 0x0d];
            if (LF) cmd = [.. cmd, 0x0a];
            if (IsConnect)
                if (TestTopPort != null)
                    await TestTopPort.SendAsync(cmd);
                else if (TestTopPort_Server != null)
                    await TestTopPort_Server.SendAsync(ClientId, cmd);
                else if (TestTopPort_M2M != null)
                    await TestTopPort_M2M.SendAsync(HostName!, Port, cmd);
        }

        private bool CanSend()
        {
            return IsConnect && !string.IsNullOrEmpty(SendData);
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