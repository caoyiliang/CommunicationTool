using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;

namespace CommunicationTool.Model
{
    public partial class CommunicationData(byte[] bytes, DataType showType, TransferDirection transferDirection = TransferDirection.Request) : ObservableObject
    {
        public DateTime DateTime { get; } = DateTime.Now;

        public string Message
        {
            get
            {
                try
                {
                    return ShowType switch
                    {
                        DataType.Ascii => Encoding.ASCII.GetString(bytes),
                        DataType.Utf8 => Encoding.UTF8.GetString(bytes),
                        DataType.Gb2312 => Encoding.GetEncoding("GB2312").GetString(bytes),
                        _ => Utils.StringByteUtils.BytesToString(bytes),
                    };
                }
                catch
                {
                    return $"不适合该编码:{Utils.StringByteUtils.BytesToString(bytes)}";
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Message))]
        private DataType _showType = showType;

        [ObservableProperty]
        private TransferDirection _transferDirection = transferDirection;
    }
}
