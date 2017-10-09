using System;
using System.Collections.Generic;

namespace ServiceStack.Configuration
{
    public class EnvironmentVariableSettings : AppSettingsBase
    {
        class EnvironmentSettingsWrapper : ISettingsReader
        {
            public string Get(string key)
            {
                return Environment.GetEnvironmentVariable(key);
            }

            public IEnumerable<string> GetAllKeys()
            {
                return Environment.GetEnvironmentVariables().Keys.Map(x => x.ToString());
            }
        }

        public EnvironmentVariableSettings() : base(new EnvironmentSettingsWrapper()) { }
    }
}