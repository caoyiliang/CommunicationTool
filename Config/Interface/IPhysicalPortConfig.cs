using System.ComponentModel;

namespace Config.Interface
{
    public interface IPhysicalPortConfig
    {
        string Info { get; }
        TestType Type { get; }

        event PropertyChangedEventHandler? PropertyChanged;
    }

}
