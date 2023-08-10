using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Common.Providers;

namespace ExpertService.Validators
{
    public class ConfigurationValidator : IConfigurationValidator
    {
        public event EventHandler<bool> ConfigurationState;

        private readonly IConfigurationProvider _configurationProvider;
        private Timer _timer;

        public ConfigurationValidator(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        public async Task StartsExpertsRealTimeValidate()
        {
            _timer = new Timer(60000)
            {
                AutoReset = true
            };

            _timer.Elapsed += ValidateAndList;
            _timer.Start();
        }

        public async Task StopSync()
        {
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }

        private void ValidateAndList(object sender, ElapsedEventArgs e)
        {
            if (IsConfigurationUpdated())
            {
                ConfigurationState(this, true);
            }
        }

        private bool IsConfigurationUpdated()
        {
            var lastModified = File.GetLastWriteTime(_configurationProvider.GetConfigurationPath());
            return (DateTime.Now - lastModified).Minutes < 1;
        }
    }
}
