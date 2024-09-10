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
    }
}
