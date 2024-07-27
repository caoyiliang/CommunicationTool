using CommunityToolkit.Mvvm.ComponentModel;
using Config;
using Config.Model;

namespace CommunicationTool.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private IEnumerable<TestType> _testTypes;
        [ObservableProperty]
        private TestType _selectedTestType;
        [ObservableProperty]
        private Connection _connection;

        public MainViewModel(ConfigManager configManager)
        {
            TestTypes = Enum.GetValues<TestType>();
            Connection = configManager.Connection;
        }
    }
}
