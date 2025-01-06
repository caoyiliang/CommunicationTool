using CommunityToolkit.Mvvm.ComponentModel;
using Config.Interface;

namespace Config.Model
{
    public partial class TcpServerConfig : ObservableObject, IPhysicalPortConfig
    {
        [ObservableProperty]
        private string _HostName = "0.0.0.0";
        [ObservableProperty]
        private int _Port = 7778;

        public string Info => $"{HostName}:{Port}";

        public TestType Type => TestType.TcpServer;

        public override string ToString() => $"当前[连接方式:{Type}] [{Info}]";
    }
}
