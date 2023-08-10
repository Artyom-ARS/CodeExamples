using System;
using System.Collections.Specialized;

namespace FxConnect
{
    public class LoginParameters
    {
        public string Login { get; }

        public string Password { get; }

        public string Url { get; }

        public string Connection { get; }

        public LoginParameters(NameValueCollection args)
        {
            Login = GetRequiredArgument(args, "Login");
            Password = GetRequiredArgument(args, "Password");
            Url = GetRequiredArgument(args, "URL");
            Connection = GetRequiredArgument(args, "Connection");
            if (!string.IsNullOrEmpty(Url))
            {
                if (!Url.EndsWith("Hosts.jsp", StringComparison.OrdinalIgnoreCase))
                {
                    Url += "/Hosts.jsp";
                }
            }
        }

        private string GetRequiredArgument(NameValueCollection args, string sArgumentName)
        {
            var sArgument = args[sArgumentName];
            if (!string.IsNullOrEmpty(sArgument))
            {
                sArgument = sArgument.Trim();
            }

            if (string.IsNullOrEmpty(sArgument))
            {
                throw new Exception($"Please provide {sArgumentName} in configuration file");
            }

            return sArgument;
        }
    }
}
