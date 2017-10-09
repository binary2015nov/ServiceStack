using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ServiceStack.Configuration
{
    /// <summary>
    /// Provides access to the System.Configuration.AppSettingsSection data for the current application's default configuration.
    /// </summary>
    public class AppSettings : AppSettingsBase
    {
        private class ConfigurationManagerWrapper : ISettingsReader
        {
#if NETSTANDARD1_6
            private Dictionary<string, string> appSettings;
            public Dictionary<string, string> AppSettings { get { return appSettings ?? (appSettings = ConfigUtils.GetAppSettingsMap()); } }
#endif

            public string Get(string key)
            {
#if !NETSTANDARD1_6
                return ConfigurationManager.AppSettings[key];
#else
                string value;
                return AppSettings.TryGetValue(key, out value)
                    ? value
                    : null;
#endif
            }

            public IEnumerable<string> GetAllKeys()
            {
#if !NETSTANDARD1_6
                return ConfigurationManager.AppSettings.AllKeys;
#else
                return AppSettings.Keys;
#endif
            }
        }

        /// <summary>
        /// Initializes a new instance of the ServiceStack.Configuration.AppSettings class.
        /// </summary>
        /// <param name="tier">The tier used to retrieve a setting. The default value is null.</param>
        public AppSettings(string tier = null) : base(new ConfigurationManagerWrapper())
        {
            Tier = tier;
        }
    }
}