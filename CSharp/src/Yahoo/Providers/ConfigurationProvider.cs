using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Yahoo.Providers
{
    public class ConfigurationProvider
    {
        public T GetConfiguration<T>()
        {
            var json = File.ReadAllText(GetConfigurationPath());
            var conf = JsonConvert.DeserializeObject<T>(json);

            return conf;
        }

        public string GetConfigurationPath()
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"config.json");
            return path;
        }
    }
}
