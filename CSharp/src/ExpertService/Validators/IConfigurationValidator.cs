using System;
using System.Threading.Tasks;

namespace ExpertService.Validators
{
    public interface IConfigurationValidator
    {
        Task StartsExpertsRealTimeValidate();

        event EventHandler<bool> ConfigurationState;

        Task StopSync();
    }
}