﻿#if NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Configuration;
using ServiceStack.Logging;

namespace ServiceStack
{
    public partial class PlatformNetCore : Platform
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PlatformNetCore));
        
        const string ErrorAppsettingNotFound = "Unable to find App Setting: {0}";
        public const string ConfigNullValue = "{null}";
        
        public static readonly List<string> AppConfigPaths = new List<string> {
            "~/web.config",
            "~/app.config",
            "~/Web.config",
            "~/App.config",
        };

        public override string GetAppConfigPath()
        {
            var host = HostInstance;

            if (host == null) 
                return null;
            
            var appConfigPaths = new List<string>(AppConfigPaths);

            try
            {
                //dll App.config
                var location = host.GetType().GetAssembly().Location;
                if (!string.IsNullOrEmpty(location))
                {
                    var appHostDll = new FileInfo(location).Name;
                    appConfigPaths.Add($"~/{appHostDll}.config");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("GetAppConfigPath() GetAssembly().Location: ", ex);
            }

            foreach (var configPath in appConfigPaths)
            {
                try
                {
                    var resolvedPath = host.MapProjectPath(configPath);
                    if (File.Exists(resolvedPath))
                        return resolvedPath;

                    resolvedPath = configPath.MapAbsolutePath();
                    if (File.Exists(resolvedPath))
                        return resolvedPath;
                }
                catch (Exception ex)
                {
                    Logger.Error("GetAppConfigPath(): ", ex);
                }
            }

            return null;
        }

        public override string GetNullableAppSetting(string key)
        {
            return ConfigUtils.GetAppSettingsMap().TryGetValue(key, out var value)
                ? value
                : null;
        }

        public override string GetAppSetting(string key)
        {
            string value = GetNullableAppSetting(key);

            if (value == null)
                throw new System.Configuration.ConfigurationErrorsException(string.Format(ErrorAppsettingNotFound, key));

            return value;
        }

        public override string GetAppSetting(string key, string defaultValue)
        {
            return GetNullableAppSetting(key) ?? defaultValue;
        }

        public override string GetConnectionString(string key)
        {
            return null;
        }

        public override T GetAppSetting<T>(string key, T defaultValue)
        {
            string val = GetNullableAppSetting(key);
            if (val != null)
            {
                if (ConfigNullValue.EndsWith(val))
                    return default(T);

                return ParseTextValue<T>(val);
            }
            return defaultValue;
        }
    }
}

#endif
