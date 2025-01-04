using CommunityToolkit.Mvvm.ComponentModel;
using Config.Interface;
using System.IO.Ports;

namespace Config.Model
{
    public partial class SerialPortConfig : ObservableObject, IPhysicalPortConfig
    {
        [ObservableProperty]
        private string _PortName = "COM1";
        [ObservableProperty]
        private int _BaudRate = 9600;
        [ObservableProperty]
        private int _DataBits = 8;
        [ObservableProperty]
        private StopBits _StopBits = StopBits.One;
        [ObservableProperty]
        private Parity _Parity = Parity.None;
        [ObservableProperty]
        private bool _DTR = false;
        [ObservableProperty]
        private bool _RTS = false;

        public string Info => $"{PortName}";

        public TestType Type => TestType.SerialPort;

        public override string ToString()
        {
            return $"当前[连接方式:{Type}] [串口名:{PortName}][波特率:{BaudRate}][数据位:{DataBits}][停止位:{StopBits}][校验位:{Parity}][DTR:{DTR}][RTS:{RTS}]";
        }
    }
}
