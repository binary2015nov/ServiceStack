using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using MarkdownSharp;
using ServiceStack.Host;
using ServiceStack.Markdown;
using ServiceStack.Metadata;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack
{
    public class HostConfig
    {
        public const string DefaultWsdlNamespace = "http://schemas.servicestack.net/types";

        public HostConfig()
        {
            WsdlServiceNamespace = DefaultWsdlNamespace;
            ApiVersion = "1.0.0";
            EmbeddedResourceSources = new HashSet<Assembly> { GetType().GetAssembly() };
            EnableAccessRestrictions = true;
            MetadataRedirectPath = null;
            DefaultContentType = null;
            PreferredContentTypes = new List<string> {
                MimeTypes.Html, MimeTypes.Json, MimeTypes.Xml, MimeTypes.Jsv
            };
            AllowJsonpRequests = true;
            AllowRouteContentTypeExtensions = true;
            AllowNonHttpOnlyCookies = false;
            DefaultDocuments = new List<string> {
                "default.htm",
                "default.html",
                "default.cshtml",
                "default.md",
                "index.htm",
                "index.html",
                "default.aspx",
                "default.ashx",
            };
            GlobalResponseHeaders = new Dictionary<string, string> {
                { "Vary", "Accept" },
                { "X-Powered-By", Env.ServerUserAgent },
            };
            IgnoreFormatsInMetadata = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AllowFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "js", "ts", "tsx", "jsx", "css", "htm", "html", "shtm", "txt", "xml", "rss", "csv", "pdf",
                "jpg", "jpeg", "gif", "png", "bmp", "ico", "tif", "tiff", "svg",
                "avi", "divx", "m3u", "mov", "mp3", "mpeg", "mpg", "qt", "vob", "wav", "wma", "wmv",
                "flv", "swf", "xap", "xaml", "ogg", "ogv", "mp4", "webm", "eot", "ttf", "woff", "woff2", "map"
            };
            CompressFilesWithExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AllowFilePaths = new List<string> {
                "jspm_packages/**/*.json"
            };
            ForbiddenPaths = new List<string> {
                "/App_Code",
            };
            DebugAspNetHostEnvironment = Env.IsMono ? "FastCGI" : "IIS7";
            DebugHttpListenerHostEnvironment = Env.IsMono ? "XSP" : "WebServer20";
            EnableFeatures = Feature.All;
            WriteErrorsToResponse = true;
            ReturnsInnerException = true;
            DisposeDependenciesAfterUse = true;
            LogUnobservedTaskExceptions = true;
            MarkdownOptions = new MarkdownOptions();
            MarkdownBaseType = typeof(MarkdownViewBase);
            MarkdownGlobalHelpers = new Dictionary<string, Type>();
            HtmlReplaceTokens = new Dictionary<string, string>();
            AddMaxAgeForStaticMimeTypes = new Dictionary<string, TimeSpan> {
                { "image/gif", TimeSpan.FromHours(1) },
                { "image/png", TimeSpan.FromHours(1) },
                { "image/jpeg", TimeSpan.FromHours(1) },
            };
            AppendUtf8CharsetOnContentTypes = new HashSet<string> { MimeTypes.Json, };
            RouteNamingConventions = RouteNamingConvention.Default.ToList();
            MapExceptionToStatusCode = new Dictionary<Type, int>();
            OnlySendSessionCookiesSecurely = false;
            AllowSessionIdsInHttpParams = false;
            AllowSessionCookies = true;
            RestrictAllCookiesToDomain = null;
            DefaultJsonpCacheExpiration = new TimeSpan(0, 20, 0);
            MetadataVisibility = RequestAttributes.Any;
            Return204NoContentForEmptyResponse = true;
            AllowJsConfig = true;
            AllowPartialResponses = true;
            AllowAclUrlReservation = true;
            AddRedirectParamsToQueryString = false;
            RedirectToDefaultDocuments = false;
            RedirectDirectoriesToTrailingSlashes = true;
            StripApplicationVirtualPath = false;
            ScanSkipPaths = new HashSet<string> {
                "obj/",
                "bin/",
                "node_modules/",
                "jspm_packages/",
                "bower_components/",
                "wwwroot_build/",
#if !NETSTANDARD2_0 
                "wwwroot/", //Need to allow VirtualFiles access from ContentRoot Folder
#endif
            };
            RedirectPaths = new Dictionary<string, string> {
                {"/metadata/", "/metadata"},
            };
            IgnoreWarningsOnPropertyNames = new List<string> {
                Keywords.Format, Keywords.Callback, Keywords.Debug, Keywords.AuthSecret, Keywords.JsConfig,
                Keywords.IgnorePlaceHolder, Keywords.Version, Keywords.VersionAbbr, Keywords.Version.ToPascalCase(),
                Keywords.ApiKeyParam, Keywords.Code,
            };
            XmlWriterSettings = new XmlWriterSettings {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            };
            FallbackRestPath = null;
            UseHttpsLinks = false;
            UseBclJsonSerializers = false;
#if !NETSTANDARD2_0
            UseCamelCase = false;
            EnableOptimizations = false;
#else
            UseCamelCase = true;
            EnableOptimizations = true;
#endif
            DisableChunkedEncoding = false;
        }

        public string WsdlServiceNamespace { get; set; }
        public string ApiVersion { get; set; }

        private RequestAttributes metadataVisibility;
        public RequestAttributes MetadataVisibility
        {
            get => metadataVisibility;
            set => metadataVisibility = value.ToAllowedFlagsSet();
        }

        public HashSet<Assembly> EmbeddedResourceSources { get; private set; }
        public HashSet<string> EmbeddedResourceTreatAsFiles { get { return ResourceVirtualDirectory.EmbeddedResourceTreatAsFiles; } set { ResourceVirtualDirectory.EmbeddedResourceTreatAsFiles = value; } }

        public string DefaultContentType { get; set; }
        public List<string> PreferredContentTypes { get; private set; }
        public bool AllowJsonpRequests { get; set; }
        public bool AllowRouteContentTypeExtensions { get; set; }
        public bool DebugMode { get; set; }
        public bool StrictMode { get; set; }
        public string DebugAspNetHostEnvironment { get; set; }
        public string DebugHttpListenerHostEnvironment { get; set; }
        public List<string> DefaultDocuments { get; private set; }
        public bool IgnoreWarningsOnAllProperties { get; set; }
        public List<string> IgnoreWarningsOnPropertyNames { get; private set; }

        public HashSet<string> IgnoreFormatsInMetadata { get; set; }

        public HashSet<string> AllowFileExtensions { get; private set; }
        public HashSet<string> CompressFilesWithExtensions { get; set; }
        public long CompressFilesLargerThanBytes { get; set; }
        public List<string> AllowFilePaths { get; private set; }

        public List<string> ForbiddenPaths { get; private set; }

        public string WebHostUrl { get; set; }
        public string HandlerFactoryPath { get; set; }
        public string DefaultRedirectPath { get; set; }
        public string MetadataRedirectPath { get; set; }

        public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
        public string SoapServiceName { get; set; }
        public XmlWriterSettings XmlWriterSettings { get; set; }
        public bool EnableAccessRestrictions { get; set; }
        public bool UseBclJsonSerializers { get { return JsonDataContractSerializer.Instance.UseBcl; } set { JsonDataContractSerializer.Instance.UseBcl = value; } }
        
        public Dictionary<string, string> GlobalResponseHeaders { get; set; }
        public Feature EnableFeatures { get; set; }
        public bool ReturnsInnerException { get; set; }
        public bool WriteErrorsToResponse { get; set; }
        public bool DisposeDependenciesAfterUse { get; set; }
        public bool LogUnobservedTaskExceptions { get; set; }

        public MarkdownOptions MarkdownOptions { get; set; }
        public Type MarkdownBaseType { get; set; }
        public Dictionary<string, Type> MarkdownGlobalHelpers { get; set; }
        public Dictionary<string, string> HtmlReplaceTokens { get; set; }

        public HashSet<string> AppendUtf8CharsetOnContentTypes { get; private set; }

        public Dictionary<string, TimeSpan> AddMaxAgeForStaticMimeTypes { get; set; }

        public List<RouteNamingConventionDelegate> RouteNamingConventions { get; private set; }

        public Dictionary<Type, int> MapExceptionToStatusCode { get; private set; }

        public bool OnlySendSessionCookiesSecurely { get; set; }
        public bool AllowSessionIdsInHttpParams { get; set; }
        public bool AllowSessionCookies { get; set; }
        public string RestrictAllCookiesToDomain { get; set; }

        public TimeSpan DefaultJsonpCacheExpiration { get; set; }
        public bool Return204NoContentForEmptyResponse { get; set; }
        public bool AllowJsConfig { get; set; }
        public bool AllowPartialResponses { get; set; }
        public bool AllowNonHttpOnlyCookies { get; set; }
        public bool AllowAclUrlReservation { get; set; }
        public bool AddRedirectParamsToQueryString { get; set; }
        public bool RedirectToDefaultDocuments { get; set; }
        public bool StripApplicationVirtualPath { get; set; }
        public bool SkipFormDataInCreatingRequest { get; set; }

        public bool RedirectDirectoriesToTrailingSlashes { get; set; }

        //Skip scanning common VS.NET extensions
        public HashSet<string> ScanSkipPaths { get { return AbstractVirtualFileBase.ScanSkipPaths; } set { AbstractVirtualFileBase.ScanSkipPaths = value; } }

        public Dictionary<string, string> RedirectPaths { get; private set; }

        public bool UseHttpsLinks { get; set; }

        public bool UseCamelCase { get { return JsConfig.EmitCamelCaseNames; } set { JsConfig.EmitCamelCaseNames = value; } }
        public bool EnableOptimizations { get { return MemoryStreamFactory.UseRecyclableMemoryStream; } set { MemoryStreamFactory.UseRecyclableMemoryStream = value; } }

        //Disables chunked encoding on Kestrel Server
        public bool DisableChunkedEncoding { get; set; }

        public string AdminAuthSecret { get; set; }

        public FallbackRestPathDelegate FallbackRestPath { get; set; }

        private HashSet<string> razorNamespaces;
        public HashSet<string> RazorNamespaces => razorNamespaces ?? (razorNamespaces = Platform.Instance.GetRazorNamespaces());
    }
}
