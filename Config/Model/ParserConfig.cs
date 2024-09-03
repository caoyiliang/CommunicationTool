using CommunityToolkit.Mvvm.ComponentModel;

namespace Config.Model
{
    public partial class ParserConfig : ObservableObject
    {
        [ObservableProperty]
        private ParserType _parserType;
        [ObservableProperty]
        private int _Time = 20;
        [ObservableProperty]
        private string? _Head;
        [ObservableProperty]
        private LengthDataType _DataType;
        [ObservableProperty]
        private int _Length;
        [ObservableProperty]
        private bool _IsHighByteBefore;
        [ObservableProperty]
        private string? _Foot;
    }
}
