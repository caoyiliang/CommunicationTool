using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using Utils;

namespace Config.Model
{
    public partial class SendCmd : ObservableObject
    {
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

        partial void OnDisplayCmdChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Cmd = SendType switch
                {
                    DataType.ASCII => Encoding.ASCII.GetBytes(value),
                    DataType.UTF8 => Encoding.UTF8.GetBytes(value),
                    DataType.GB2312 => Encoding.GetEncoding("GB2312").GetBytes(value),
                    _ => StringByteUtils.StringToBytes(value),
                };
            }
            else
            {
                Cmd = [];
            }
        }

        partial void OnSendTypeChanged(DataType value)
        {
            OnDisplayCmdChanged(DisplayCmd);
        }
    }
}
