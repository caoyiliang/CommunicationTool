using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using Utils;

namespace Config.Model
{
    public partial class SendCmd : ObservableObject
    {
        [ObservableProperty]
        private bool _IsSelected;
        [ObservableProperty]
        private Guid _Id;
        [ObservableProperty]
        private string? _Name;
        [ObservableProperty]
        private DataType _SendType;
        [ObservableProperty]
        private byte[] _Cmd = [];
        [ObservableProperty]
        private string? _DisplayCmd;
        [ObservableProperty]
        private bool _CR;
        [ObservableProperty]
        private bool _LF;
        [ObservableProperty]
        private CrcType _CrcType;
        [ObservableProperty]
        private byte[] _SendCmds = [];

        partial void OnCmdChanged(byte[] value)
        {
            SendCmds = GetCMD();
        }

        partial void OnCRChanged(bool value)
        {
            SendCmds = GetCMD();
        }

        partial void OnLFChanged(bool value)
        {
            SendCmds = GetCMD();
        }

        partial void OnCrcTypeChanged(CrcType value)
        {
            SendCmds = GetCMD();
        }

        public byte[] GetCMD()
        {
            var cmd = CrcType switch
            {
                CrcType.Modbus => CRC.Crc16(Cmd),
                CrcType.Modbus_R => StringByteUtils.ComibeByteArray(Cmd, CRC.CRC16_R(Cmd)),
                CrcType.GB => StringByteUtils.ComibeByteArray(Cmd, CRC.GBcrc16(Cmd, Cmd.Length)),
                CrcType.GB_string => StringByteUtils.ComibeByteArray(Cmd, Encoding.GetEncoding("gb2312").GetBytes(StringByteUtils.BytesToString(CRC.GBcrc16(Cmd, Cmd.Length)).Replace(" ", ""))),
                CrcType.GB_protocol => ProcessGBProtocol(Cmd),
                CrcType.HB => StringByteUtils.ComibeByteArray(Cmd, CRC.HBcrc16(Cmd, Cmd.Length)),
                CrcType.HB_string => StringByteUtils.ComibeByteArray(Cmd, Encoding.GetEncoding("gb2312").GetBytes(StringByteUtils.BytesToString(CRC.HBcrc16(Cmd, Cmd.Length)).Replace(" ", ""))),
                CrcType.HB_protocol => ProcessHBProtocol(Cmd),
                CrcType.UCRC => StringByteUtils.ComibeByteArray(Cmd, StringByteUtils.GetBytes(CRC.UpdateCRC(Cmd, Cmd.Length), true)),
                _ => Cmd,
            };
            if (CR) cmd = [.. cmd, 0x0d];
            if (LF) cmd = [.. cmd, 0x0a];
            return cmd;
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
