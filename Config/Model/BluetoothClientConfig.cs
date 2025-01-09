using CommunityToolkit.Mvvm.ComponentModel;
using Config.Interface;

namespace Config.Model
{
    public partial class BluetoothClientConfig : ObservableObject, IPhysicalPortConfig
    {
        public string Info => "BluetoothClient";

        public TestType Type => TestType.ClassicBluetoothClient;

        public override string ToString() => $"当前[连接方式:{Type}]";
    }
}
