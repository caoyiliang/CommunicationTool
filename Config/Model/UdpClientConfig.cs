using CommunityToolkit.Mvvm.ComponentModel;
using Config.Interface;

namespace Config.Model
{
    public partial class UdpClientConfig : ObservableObject, IPhysicalPortConfig
    {
        [ObservableProperty]
        private string _HostName = "127.0.0.1";
        [ObservableProperty]
        private int _Port = 7778;
        [ObservableProperty]
        private string _RemoteHostName = "127.0.0.1";
        [ObservableProperty]
        private int _RemotePort = 2756;

        public string Info => $"{HostName}:{Port}";

        public TestType Type => TestType.UdpClient;

        public override string ToString() => $"当前[连接方式:{Type}] [本地:{HostName}:{Port} 远程:{RemoteHostName}:{RemotePort}]";
    }
}
