using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract class Platform
    {
        public static bool IsIntegratedPipeline { get; protected set; }

        internal static ServiceStackHost HostInstance { get; set; }

        public static Platform Instance = 
#if NETSTANDARD2_0
            new PlatformNetCore();
#else
            new PlatformNet();
#endif

        public virtual HashSet<string> GetRazorNamespaces()
        {
            return new HashSet<string>();
        }

        public virtual string GetNullableAppSetting(string key)
        {
            return null;
        }

        public virtual string GetAppSetting(string key)
        {
            return null;
        }

        public virtual string GetAppSetting(string key, string defaultValue)
        {
            return defaultValue;
        }

        public virtual T GetAppSetting<T>(string key, T defaultValue)
        {
            return defaultValue;
        }

        public virtual string GetConnectionString(string key)
        {
            return null;
        }

        public virtual string GetAppConfigPath()
        {
            return null;
        }

        public virtual Dictionary<string, string> GetCookiesAsDictionary(IRequest httpReq)
        {
            return new Dictionary<string, string>();
        }

        public virtual Dictionary<string, string> GetCookiesAsDictionary(IResponse httpRes)
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Get the static Parse(string) method on the type supplied
        /// </summary>
        private static MethodInfo GetParseMethod(Type type)
        {
            const string parseMethod = "Parse";
            if (type == typeof(string))
                return typeof(ConfigUtils).GetMethod(parseMethod, BindingFlags.Public | BindingFlags.Static);

            var parseMethodInfo = type.GetStaticMethod(parseMethod, new[] { typeof(string) });
            return parseMethodInfo;
        }

        /// <summary>
        /// Gets the constructor info for T(string) if exists.
        /// </summary>
        private static ConstructorInfo GetConstructorInfo(Type type)
        {
            foreach (var ci in type.GetConstructors())
            {
                var ciTypes = ci.GetGenericArguments();
                var matchFound = (ciTypes.Length == 1 && ciTypes[0] == typeof(string)); //e.g. T(string)
                if (matchFound)
                    return ci;
            }
            return null;
        }

        /// <summary>
        /// Returns the value returned by the 'T.Parse(string)' method if exists otherwise 'new T(string)'. 
        /// e.g. if T was a TimeSpan it will return TimeSpan.Parse(textValue).
        /// If there is no Parse Method it will attempt to create a new instance of the destined type
        /// </summary>
        public static T ParseTextValue<T>(string textValue)
        {
            var parseMethod = GetParseMethod(typeof(T));
            if (parseMethod == null)
            {
                var ci = GetConstructorInfo(typeof(T));
                if (ci == null)
                    throw new TypeLoadException(
                        $"Error creating type {typeof(T).GetOperationName()} from text '{textValue}");

                var newT = ci.Invoke(null, new object[] { textValue });
                return (T)newT;
            }
            var value = parseMethod.Invoke(null, new object[] { textValue });
            return (T)value;
        }

        public static int FindFreeTcpPort(int startingFrom = 5000, int endingAt = 65535)
        {
            var tcpEndPoints = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            var activePorts = new HashSet<int>();
            foreach (var endPoint in tcpEndPoints)
            {
                activePorts.Add(endPoint.Port);
            }

            for (var port = startingFrom; port < endingAt; port++)
            {
                if (!activePorts.Contains(port))
                    return port;
            }

            return -1;
        }

        public static DateTime GetAssemblyLastModified(Assembly assembly)
        {
            if (assembly.Location == string.Empty)
                throw new NotSupportedException("The current assembly is loaded from a byte array");

            return new FileInfo(assembly.Location).LastWriteTime;           
        }
    }
}