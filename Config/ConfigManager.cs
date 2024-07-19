using Config.Model;
using System.Collections.Concurrent;
using System.Reflection;

namespace Config
{
    public class ConfigManager
    {
        public Connection Connection { get; set; } = null!;
        public static ConfigManager Instance { get { return lazy.Value; } }

        private static readonly Lazy<ConfigManager> lazy = new(() => new ConfigManager());

        private ConfigManager() { }

        public async Task<ConfigManager> InitAsync()
        {
            var errors = new ConcurrentBag<string>();
            var tasks = new List<Task>();
            foreach (var item in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var obj = Activator.CreateInstance(item.PropertyType);
                if (obj is null) continue;
                var func = obj.GetType().GetMethod("InitAsync");
                if (func is null) continue;
                var task = (Task?)func.Invoke(obj, null);
                if (task is null) continue;
                tasks.Add(task.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        errors.Add($"{item.Name} 初始化配置失败:\n {t.Exception}");
                    }
                    else
                    {
                        item.SetValue(this, obj);
                    }
                }));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);

            if (!errors.IsEmpty)
            {
                throw new Exception(string.Join("\n\n", errors));
            }
            return this;
        }
    }
}
