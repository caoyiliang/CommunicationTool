using CommunicationTool.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Config;
using System.Collections.ObjectModel;

namespace CommunicationTool.ViewModel
{
    public partial class ReceiveViewModel : ObservableObject
    {
        [ObservableProperty]
        private DataType _SelectedShowType = DataType.HEX;
        [ObservableProperty]
        private bool _AutoScroll = true;
        [ObservableProperty]
        private Int128 _rsponseLength;
        [ObservableProperty]
        private Int128 _requestLength;

        public ObservableCollection<CommunicationData> CommunicationDatas { get; set; } = [];

        partial void OnSelectedShowTypeChanged(DataType value)
        {
            foreach (var item in CommunicationDatas)
            {
                item.ShowType = value;
            }
        }

        [RelayCommand]
        private void ClearRsponseLength()
        {
            RsponseLength = 0;
        }

        [RelayCommand]
        private void ClearRequestLength()
        {
            RequestLength = 0;
        }

        [RelayCommand]
        private void Clear()
        {
            RequestLength = 0;
            RsponseLength = 0;
            CommunicationDatas.Clear();
        }
    }
}