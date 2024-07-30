using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;

namespace CommunicationTool.Model
{
    public partial class CommunicationData(byte[] bytes, RecType showType) : ObservableObject
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
                        RecType.Ascii => Encoding.ASCII.GetString(bytes),
                        RecType.Utf8 => Encoding.UTF8.GetString(bytes),
                        RecType.Gb2312 => Encoding.GetEncoding("GB2312").GetString(bytes),
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
        private RecType _showType = showType;
    }
}
