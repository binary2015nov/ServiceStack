using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;

namespace ServiceStack
{
    public class SimpleAppSettings : IAppSettings
    {
        private readonly Dictionary<string, string> settings;

        public SimpleAppSettings(Dictionary<string, string> settings = null) =>
            this.settings = settings ?? new Dictionary<string, string>();

        public Dictionary<string, string> GetAll() => settings;

        public IEnumerable<string> GetAllKeys() => settings.Keys;

        public bool Exists(string key) => settings.ContainsKey(key);

        public void Set<T>(string key, T value)
        {
            var s = value as string;
            var textValue = s != null
                ? (string)(object)value
                : value.ToJsv();

            settings[key] = textValue;
        }

        public string Get(string key) => settings.TryGetValue(key, out string value)
            ? value
            : null;

        public IList<string> GetList(string key) => Get(key).FromJsv<List<string>>();

        public IDictionary<string, string> GetDictionary(string key) => Get(key).FromJsv<Dictionary<string, string>>();

        public T Get<T>(string key) => Get(key).FromJsv<T>();

        public T Get<T>(string key, T defaultValue)
        {
            var value = Get(key);
            return value != null ? value.FromJsv<T>() : defaultValue;
        }
    }
}