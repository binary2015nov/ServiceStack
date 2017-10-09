using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ServiceStack.Text;

namespace ServiceStack.Configuration
{
    public delegate string ParsingStrategyDelegate(string originalSetting);

    /// <summary>
    /// Provides a collection of keys and values that contains application settings. This is an abstract class.
    /// </summary>
    public abstract class AppSettingsBase : IAppSettings
    {
        protected ISettingsReader SettingsReader;

        protected ISettingsWriter SettingsWriter;

        /// <summary>
        /// Gets or sets the prefix of key, which lets you retrieve a setting with the tier first before falling back to the original key. 
        /// E.g a tier of 'Live' looks for 'Live.{Key}' or if not found falls back to '{Key}'.
        /// </summary>
        public string Tier { get; set; }

        public ParsingStrategyDelegate ParsingStrategy { get; set; }

        /// <summary>
        /// Initializes a new instance of the ServiceStack.Configuration.AppSettingsBase class using the specified settings reader.
        /// </summary>
        /// <param name="reader">The instance of class to read the settings.</param>
        protected AppSettingsBase(ISettingsReader reader = null)
        {
            SettingsReader = reader;
            SettingsWriter = (reader as ISettingsWriter) ?? new DictionarySettings();
        }

        /// <summary>
        /// Gets the string value associated with the specified key.
        /// </summary>
        /// <param name="key">The specified key.</param>
        /// <returns>The string value associated with the specified key. If the specified key is not found, return null.</returns>
        public virtual string Get(string key)
        {
            return GetNullableString(key);
        }

        public virtual IEnumerable<string> GetAllKeys()
        {
            var keys = SettingsWriter.GetAllKeys().ToHashSet();
            if (SettingsReader != SettingsWriter)
                SettingsReader.GetAllKeys().Each(x => keys.Add(x));

            return keys;
        }

        public virtual Dictionary<string, string> GetAll()
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var key in GetAllKeys())
            {
                dictionary[key] = Get(key);
            }
            return dictionary;
        }

        public virtual bool Exists(string key)
        {
            if (SettingsWriter.GetAllKeys().Contains(key))
                return true;

            if (SettingsWriter != SettingsReader)
                return SettingsReader.GetAllKeys().Contains(key);

            return false;
        }

        public virtual IList<string> GetList(string key)
        {
            var value = Get(key);
            return value == null
                ? new List<string>()
                : ConfigUtils.GetListFromAppSettingValue(value);
        }

        public virtual IDictionary<string, string> GetDictionary(string key)
        {
            try
            {
                var value = Get(key);
                return ConfigUtils.GetDictionaryFromAppSettingValue(value);
            }
            catch (Exception ex)
            {
                var message = $"The {key} setting had an invalid dictionary format. " +
                              $"The correct format is of type \"Key1:Value1,Key2:Value2\"";
                throw new ConfigurationErrorsException(message, ex);
            }
        }

        public virtual T Get<T>(string key)
        {
            return Get(key, default(T));
        }

        public virtual T Get<T>(string key, T defaultValue)
        {
            try
            {
                var value = Get(key);
                return value != null ? TypeSerializer.DeserializeFromString<T>(value) : defaultValue;            
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(
                    $"The {key} setting had an invalid format, could not be cast to the type {typeof(T).FullName}.", ex);
            }
        }

        public virtual void Set<T>(string key, T value)
        {
            SettingsWriter.Set(key, value);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetRaw(string key)
        {
            return SettingsWriter.Get(key) ?? SettingsReader.Get(key);
        }

        protected virtual string GetNullableString(string key)
        {
            var value = Tier != null ? GetRaw($"{Tier}.{key}") ?? GetRaw(key) : GetRaw(key);

            return ParsingStrategy != null
                ? ParsingStrategy(value)
                : value;
        }
    }

    public static class AppSettingsStrategy
    {
        public static string CollapseNewLines(string originalSetting)
        {
            if (originalSetting == null) return null;

            var lines = originalSetting.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 1 
                ? string.Join("", lines.Select(x => x.Trim())) 
                : originalSetting;
        }
    }
}