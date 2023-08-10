using System.Configuration;

namespace FxConnect.Providers
{
    public class ParametersProvider : IParametersProvider
    {
        public LoginParameters LoginParameters
        {
            get
            {
                var loginParams = new LoginParameters(ConfigurationManager.AppSettings);
                return loginParams;
            }
        }
    }
}
