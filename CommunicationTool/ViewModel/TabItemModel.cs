using CommunicationTool.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace CommunicationTool.Model
{
    public partial class TabItemViewModel(Guid clientId) : ObservableObject
    {
        [ObservableProperty]
        private Guid _clientId = clientId;
        [ObservableProperty]
        private bool _IsConnect;
        [ObservableProperty]
        private string? _Header;
        [ObservableProperty]
        private ReceiveViewModel _ReceiveViewModel = new();

        [RelayCommand]
        private void Close()
        {
            WeakReferenceMessenger.Default.Send(new CloseTabItemMessage(ClientId));
        }
    }
}
