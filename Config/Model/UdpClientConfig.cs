using CommunityToolkit.Mvvm.ComponentModel;
using Config.Interface;

namespace Config.Model
{
    public partial class UdpClientConfig : ObservableObject, IPhysicalPortConfig
    {
        [ObservableProperty]
        private string _HostName = "0.0.0.0";
        [ObservableProperty]
        private int _Port = 7778;
        [ObservableProperty]
        private string _RemoteHostName = "192.168.1.13";
        [ObservableProperty]
        private int _RemotePort = 2756;

        public string Info => $"本地:{HostName}:{Port} 远程:{RemoteHostName}:{RemotePort}";

        public TestType Type => TestType.TcpServer;

        public override string ToString() => $"当前[连接方式:{Type}] [{Info}]";
    }
}
