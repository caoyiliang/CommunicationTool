using CommunityToolkit.Mvvm.ComponentModel;
using DataPairs;
using DataPairs.Interfaces;
using System.Collections.ObjectModel;

namespace Config.Model
{
    public partial class Connection : ObservableObject
    {
        public ObservableCollection<SerialPortTest> SerialPortTests { get; set; } = [];

        private static readonly IDataPair<Connection> _pair = new DataPair<Connection>(nameof(Connection), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PairsDB.dll"));

        public async Task InitAsync()
        {
            var data = await _pair.TryGetValueAsync();
            foreach (var item in data.GetType().GetProperties())
            {
                GetType().GetProperty(item.Name)!.SetValue(this, item.GetValue(data));
            }
        }

        public async Task TrySaveChangeAsync()
        {
            await _pair.TryInitOrUpdateAsync(this);
        }
    }
}
