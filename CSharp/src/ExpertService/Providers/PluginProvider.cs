using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Common.Constants;
using Common.Experts;
using Common.Providers;
using ExpertService.Models;

namespace ExpertService.Providers
{
    public class PluginProvider : IPluginProvider
    {
        private readonly IConfigurationProvider _configurationProvider;

        public PluginProvider(IConfigurationProvider configurationLoader)
        {
            _configurationProvider = configurationLoader;
        }

        public async Task<List<IExpert>> ListExperts()
        {
            var expertsConfigurations = _configurationProvider.GetConfiguration<ExpertsConfigurations>();
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var experts = new List<IExpert>();

            foreach (var expertConfiguration in expertsConfigurations.Experts)
            {
                if (expertConfiguration.Disabled)
                {
                    continue;
                }
                var fullPath = $"{path}\\{expertConfiguration.AssemblyName}{PlatformParameters.ConfigurationFileExtension}";
                var assembly = Assembly.LoadFile(fullPath);
                var type = assembly.GetType($"{expertConfiguration.AssemblyName}.{expertConfiguration.ClassName}");
                var expert = (IExpert)Activator.CreateInstance(type);
                expert.InitializeId(expertConfiguration.Id);
                experts.Add(expert);
            }

            return experts;
        }
    }
}
