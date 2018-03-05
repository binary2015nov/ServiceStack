using System;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    /// <summary>
    /// Context to capture IService action
    /// </summary>
    public sealed class ActionContext
    {
        public const string AnyAction = "ANY";

        public string Id { get; set; }
        public Type RequestType { get; set; }
        public Type ServiceType { get; set; }
        public Type ResponseType { get; set; }

        public ActionInvokerFn ServiceAction { get; set; }
        public IRequestFilterBase[] RequestFilters { get; set; }
        public IResponseFilterBase[] ResponseFilters { get; set; }

        public static string Key(string method, string requestDtoName)
        {
            return method.ToUpper() + " " + requestDtoName;
        }

        public static string AnyKey(string requestDtoName)
        {
            return AnyAction + " " + requestDtoName;
        }

        public static string AnyFormatKey(string format, string requestDtoName)
        {
            return AnyAction + format + " " + requestDtoName;
        }
    }
}