using System.Collections.Generic;

namespace ServiceStack.Configuration
{
    public interface IAppSettings : ISettingsWriter
    {
        Dictionary<string, string> GetAll();

        bool Exists(string key);

        IList<string> GetList(string key);

        IDictionary<string, string> GetDictionary(string key);

        T Get<T>(string key);

        T Get<T>(string key, T defaultValue);
    }
}