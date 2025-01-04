using CommunityToolkit.Mvvm.ComponentModel;
using Config.Interface;

namespace Config.Model
{
    public partial class TcpClientConfig : ObservableObject, IPhysicalPortConfig
    {
        [ObservableProperty]
        private string _HostName = "127.0.0.1";
        [ObservableProperty]
        private int _Port = 7778;

        public string Info => $"{HostName}:{Port}";

        public TestType Type => TestType.TcpClient;

        public override string ToString() => $"当前[连接方式:{Type}] [{Info}]";
    }
}
