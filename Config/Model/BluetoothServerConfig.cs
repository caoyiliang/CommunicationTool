using CommunityToolkit.Mvvm.ComponentModel;
using Config.Interface;

namespace Config.Model
{
    public partial class BluetoothServerConfig : ObservableObject, IPhysicalPortConfig
    {
        public string Info => "BluetoothServer";

        public TestType Type => TestType.ClassicBluetoothServer;

        public override string ToString() => $"当前[连接方式:{Type}]";
    }
}
