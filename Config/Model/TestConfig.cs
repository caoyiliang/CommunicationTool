using CommunityToolkit.Mvvm.ComponentModel;
using Config.Interface;
using System.Collections.ObjectModel;

namespace Config.Model
{
    public partial class TestConfig : ObservableObject
    {
        [ObservableProperty]
        private Guid _Id = Guid.NewGuid();
        [ObservableProperty]
        private string? _TestName;
        [ObservableProperty]
        private IPhysicalPortConfig _PhysicalPortConnection = null!;
        [ObservableProperty]
        private ParserConfig _ParserConfig = new();
        [ObservableProperty]
        private ObservableCollection<SendCmd> _SendCmds = [];
        [ObservableProperty]
        private bool _IsAutoSend;
        [ObservableProperty]
        private int _SendInterval = 1000;

        public TestConfig(IPhysicalPortConfig physicalPortConnection)
        {
            _TestName = $"{physicalPortConnection.Type}Test";
            _PhysicalPortConnection = physicalPortConnection;
        }

        public TestConfig() { }
    }
}
