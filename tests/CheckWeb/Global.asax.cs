﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Check.ServiceInterface;
using Check.ServiceModel;
using Check.ServiceModel.Types;
using Funq;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Api.OpenApi;
using ServiceStack.Api.OpenApi.Specification;
using ServiceStack.Api.Swagger;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.Formats;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.VirtualPath;
using ServiceStack.Web;

namespace CheckWeb
{
    public class AppHost : AppHostBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppHost"/> class.
        /// </summary>
        public AppHost() : base("CheckWeb", typeof(ErrorsService).Assembly, typeof(HtmlServices).Assembly)
        {
            // Change ServiceStack configuration
            Config.DebugMode = true;
            //Config.UseHttpsLinks = true;
            Config.AppendUtf8CharsetOnContentTypes.Add(MimeTypes.Html);
            Config.UseCamelCase = true;
            Config.AdminAuthSecret = "secretz";
            //Config.AllowJsConfig = false;

            // Set to return JSON if no request content type is defined
            // e.g. text/html or application/json
            //Config.DefaultContentType = MimeTypes.Json;
            // Disable SOAP endpoints
            //Config.EnableFeatures = Feature.All.Remove(Feature.Soap);
            //Config.EnableFeatures = Feature.All.Remove(Feature.Metadata);
  
        }

        /// <summary>
        /// Configure the Web Application host.
        /// </summary>
        /// <param name="container">The container.</param>
        public override void Configure(Container container)
        {
            this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = new RazorHandler("/Views/TestErrorNotFound");

            var nativeTypes = this.GetPlugin<NativeTypesFeature>();
            nativeTypes.MetadataTypesConfig.ExportTypes.Add(typeof(DayOfWeek));
            nativeTypes.MetadataTypesConfig.IgnoreTypes.Add(typeof(IgnoreInMetadataConfig));
            //nativeTypes.MetadataTypesConfig.GlobalNamespace = "Check.ServiceInterface";

            container.Register<IServiceClient>(c =>
                new JsonServiceClient("http://localhost:55799/"));

            Plugins.Add(new TemplatePagesFeature
            {
                EnableDebugTemplateToAll = true
            });
            
//            Plugins.Add(new SoapFormat());

            //ProxyFetureTests
            Plugins.Add(new ProxyFeature(
                matchingRequests: req => req.PathInfo.StartsWith("/proxy/test"),
                resolveUrl: req => "http://test.servicestack.net".CombineWith(req.RawUrl.Replace("/test", "/"))));

            Plugins.Add(new ProxyFeature(
                matchingRequests: req => req.PathInfo.StartsWith("/techstacks"),
                resolveUrl: req => "http://techstacks.io".CombineWith(req.RawUrl.Replace("/techstacks", "/"))));

            Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });

            Plugins.Add(new AutoQueryDataFeature()
                .AddDataSource(ctx => ctx.MemorySource(GetRockstars())));

            Plugins.Add(new AdminFeature());

            Plugins.Add(new PostmanFeature());
            Plugins.Add(new CorsFeature(
                allowOriginWhitelist: new[] { "http://localhost", "http://localhost:8080", "http://localhost:56500", "http://test.servicestack.net", "http://null.jsbin.com" },
                allowCredentials: true,
                allowedHeaders: "Content-Type, Allow, Authorization, X-Args"));

            Plugins.Add(new ServerEventsFeature
            {
                LimitToAuthenticatedUsers = true
            });

            GlobalRequestFilters.Add((req, res, dto) =>
            {
                if (dto is AlwaysThrowsGlobalFilter)
                    throw new Exception(dto.GetType().Name);
            });

            Plugins.Add(new RequestLogsFeature
            {
                RequestLogger = new CsvRequestLogger(),
            });

            Plugins.Add(new DynamicallyRegisteredPlugin());

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
            //container.Register<IDbConnectionFactory>(
            //    new OrmLiteConnectionFactory("Server=localhost;Database=test;User Id=test;Password=test;", SqlServerDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Rockstar>();
                db.InsertAll(GetRockstars());
                
                db.DropAndCreateTable<AllTypes>();
                db.Insert(new AllTypes
                {
                    Id = 1,
                    Int = 2,
                    Long = 3,
                    Float = 1.1f,
                    Double = 2.2,
                    Decimal = 3.3m,
                    DateTime = DateTime.Now,
                    Guid = Guid.NewGuid(),
                    TimeSpan = TimeSpan.FromMilliseconds(1),
                    String = "String"
                });
            }
            
            Plugins.Add(new MiniProfilerFeature());

            var dbFactory = (OrmLiteConnectionFactory)container.Resolve<IDbConnectionFactory>();
            dbFactory.RegisterConnection("SqlServer",
                new OrmLiteConnectionFactory(
                    @"Server=.\SQLEXPRESS;Database=test;",
                    SqlServerDialect.Provider)
                {
                    ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                });

            dbFactory.RegisterConnection("pgsql",
                new OrmLiteConnectionFactory(
                    "Server=localhost;Port=5432;User Id=postgres;Password=admin;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200",
                    PostgreSqlDialect.Provider));

            using (var db = dbFactory.OpenDbConnection("pgsql"))
            {
                db.DropAndCreateTable<Rockstar>();
                db.DropAndCreateTable<PgRockstar>();

                db.Insert(new Rockstar { Id = 1, FirstName = "PostgreSQL", LastName = "Connection", Age = 1 });
                db.Insert(new PgRockstar { Id = 1, FirstName = "PostgreSQL", LastName = "Named Connection", Age = 1 });
            }

            //this.GlobalHtmlErrorHttpHandler = new RazorHandler("GlobalErrorHandler.cshtml");

            // Configure JSON serialization properties.
            this.ConfigureSerialization(container);

            // Configure ServiceStack database connections.
            this.ConfigureDataConnection(container);

            // Configure ServiceStack Authentication plugin.
            this.ConfigureAuth(container);

            // Configure ServiceStack Fluent Validation plugin.
            this.ConfigureValidation(container);

            // Configure ServiceStack Razor views.
            this.ConfigureView(container);

            this.StartUpErrors.Add(new ResponseStatus("Mock", "Startup Error"));

            //PreRequestFilters.Add((req, res) =>
            //{
            //    if (req.PathInfo.StartsWith("/metadata") || req.PathInfo.StartsWith("/swagger-ui"))
            //    {
            //        var session = req.GetSession();
            //        if (!session.IsAuthenticated)
            //        {
            //            res.StatusCode = (int)HttpStatusCode.Unauthorized;
            //            res.EndRequest();
            //        }
            //    }
            //});
        }

        public static Rockstar[] GetRockstars()
        {
            return new[]
            {
                new Rockstar {Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27},
                new Rockstar {Id = 2, FirstName = "Jim", LastName = "Morrison", Age = 27},
                new Rockstar {Id = 3, FirstName = "Kurt", LastName = "Cobain", Age = 27},
                new Rockstar {Id = 4, FirstName = "Elvis", LastName = "Presley", Age = 42},
                new Rockstar {Id = 5, FirstName = "David", LastName = "Grohl", Age = 44},
                new Rockstar {Id = 6, FirstName = "Eddie", LastName = "Vedder", Age = 48},
                new Rockstar {Id = 7, FirstName = "Michael", LastName = "Jackson", Age = 50},
            };
        }

        /// <summary>
        /// Configure JSON serialization properties.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureSerialization(Container container)
        {
            // Set JSON web services to return idiomatic JSON camelCase properties
            //JsConfig.EmitCamelCaseNames = true;
            //JsConfig.EmitLowercaseUnderscoreNames = true;

            // Set JSON web services to return ISO8601 date format
            JsConfig.DateHandler = DateHandler.ISO8601;

            // Exclude type info during serialization as an effect of IoC
            JsConfig.ExcludeTypeInfo = true;
        }

        /// <summary>
        /// // Configure ServiceStack database connections.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureDataConnection(Container container)
        {
            // ...
        }

        /// <summary>
        /// Configure ServiceStack Authentication plugin.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureAuth(Container container)
        {
            Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new IAuthProvider[]
                {
                    new CredentialsAuthProvider(AppSettings),
                    new JwtAuthProvider(AppSettings)
                    {
                        AuthKey = Convert.FromBase64String("3n/aJNQHPx0cLu/2dN3jWf0GSYL35QlMqgz+LH3hUyA="),
                        RequireSecureConnection = false,
                    }, 
                    new ApiKeyAuthProvider(AppSettings),
                    new BasicAuthProvider(AppSettings),
                }));
            
            Plugins.Add(new RegistrationFeature());

            var authRepo = new OrmLiteAuthRepository(container.Resolve<IDbConnectionFactory>());
            container.Register<IAuthRepository>(c => authRepo);
            authRepo.InitSchema();

            authRepo.CreateUserAuth(new UserAuth
            {
                UserName = "test",
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
            }, "test");
        }

        /// <summary>
        /// Configure ServiceStack Fluent Validation plugin.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureValidation(Container container)
        {
            // Provide fluent validation functionality for web services
            Plugins.Add(new ValidationFeature());

            container.RegisterValidators(typeof(AppHost).Assembly);
            container.RegisterValidators(typeof(ThrowValidationValidator).Assembly);
        }

        /// <summary>
        /// Configure ServiceStack Razor views.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureView(Container container)
        {
            // Enable ServiceStack Razor
            var razor = new RazorFormat();
            razor.Deny.RemoveAt(0);
            Plugins.Add(razor);

            Plugins.Add(new OpenApiFeature
            {
                ApiDeclarationFilter = api =>
                {
                    foreach (var path in new[] {api.Paths["/auth"], api.Paths["/auth/{provider}"]})
                    {
                        path.Get = path.Put = path.Delete = null;
                    }
                },
                Tags =
                {
                    new OpenApiTag
                    {
                        Name = "TheTag",
                        Description = "TheTag Description",
                        ExternalDocs = new OpenApiExternalDocumentation
                        {
                            Description = "Link to External Docs Desc",
                            Url = "http://example.org/docs/path",
                        }
                    }
                }
            });

            // Enable support for Swagger API browser
            //Plugins.Add(new SwaggerFeature
            //{
            //    UseBootstrapTheme = true,
            //    LogoUrl = "//lh6.googleusercontent.com/-lh7Gk4ZoVAM/AAAAAAAAAAI/AAAAAAAAAAA/_0CgCb4s1e0/s32-c/photo.jpg"
            //});
            //Plugins.Add(new CorsFeature()); // Uncomment if the services to be available from external sites
        }

        public override List<IVirtualPathProvider> GetVirtualFileSources()
        {
            var existingProviders = base.GetVirtualFileSources();
            //return existingProviders;

            var memFs = new MemoryVirtualFiles();

            //Get FileSystem Provider
            var fs = existingProviders.First(x => x is FileSystemVirtualFiles);

            //Process all .html files:
            foreach (var file in fs.GetAllMatchingFiles("*.html"))
            {
                var contents = Minifiers.HtmlAdvanced.Compress(file.ReadAllText());
                memFs.WriteFile(file.VirtualPath, contents);
            }

            //Process all .css files:
            foreach (var file in fs.GetAllMatchingFiles("*.css")
                .Where(file => !file.VirtualPath.EndsWith(".min.css")))
            {
                var contents = Minifiers.Css.Compress(file.ReadAllText());
                memFs.WriteFile(file.VirtualPath, contents);
            }

            //Process all .js files
            foreach (var file in fs.GetAllMatchingFiles("*.js")
                .Where(file => !file.VirtualPath.EndsWith(".min.js")))
            {
                try
                {
                    var js = file.ReadAllText();
                    var contents = Minifiers.JavaScript.Compress(js);
                    memFs.WriteFile(file.VirtualPath, contents);
                }
                catch (Exception ex)
                {
                    //Report any errors in StartUpErrors collection on ?debug=requestinfo
                    base.OnStartupException(new Exception("JSMin Error in {0}: {1}".Fmt(file.VirtualPath, ex.Message)));
                }
            }

            //Give new Memory FS highest priority
            existingProviders.Insert(0, memFs);
            return existingProviders;
        }
    }
    
    [Route("/query/alltypes")]
    public class QueryAllTypes : QueryDb<AllTypes> { }

    [Route("/test/html")]
    public class TestHtml : IReturn<TestHtml>
    {
        public string Name { get; set; }
    }

    [Route("/test/html2")]
    public class TestHtml2
    {
        public string Name { get; set; }
    }

    [HtmlOnly]
    [CacheResponse(Duration = 3600)]
    public class HtmlServices : Service
    {
        public object Any(TestHtml request) => request;

        public object Any(TestHtml2 request) => new HttpResult(new TestHtml { Name = request.Name })
        {
            View = nameof(TestHtml)
        };
    }

    [Route("/views/request")]
    public class ViewRequest
    {
        public string Name { get; set; }
    }

    public class ViewResponse
    {
        public string Result { get; set; }
    }

    public class ViewServices : Service
    {
        public object Get(ViewRequest request)
        {
            var result = Gateway.Send(new TestHtml());
            return new ViewResponse { Result = request.Name };
        }
    }

    [Route("/index")]
    public class IndexPage
    {
        public string PathInfo { get; set; }
    }

    [Route("/return/text")]
    public class ReturnText
    {
        public string Text { get; set; }
    }

    public class MyServices : Service
    {
        //Return default.html for unmatched requests so routing is handled on client
        public object Any(IndexPage request) =>
            new HttpResult(VirtualFileSources.GetFile("default.html"));

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(ReturnText request) => request.Text;
    }

    [Route("/plain-dto")]
    public class PlainDto : IReturn<PlainDto>
    {
        public string Name { get; set; }
    }

    [Route("/httpresult-dto")]
    public class HttpResultDto : IReturn<HttpResultDto>
    {
        public string Name { get; set; }
    }

    public class HttpResultServices : Service
    {
        public object Any(PlainDto request) => request;

        public object Any(HttpResultDto request) => new HttpResult(request, HttpStatusCode.Created);
    }

    [Route("/restrict/mq")]
    [Restrict(RequestAttributes.MessageQueue)]
    public class TestMqRestriction : IReturn<TestMqRestriction>
    {
        public string Name { get; set; }
    }

    public class TestRestrictionsService : Service
    {
        public object Any(TestMqRestriction request) => request;
    }

    [Route("/set-cache")]
    public class SetCache : IReturn<SetCache>
    {
        public string ETag { get; set; }
        public TimeSpan? Age { get; set; }
        public TimeSpan? MaxAge { get; set; }
        public DateTime? Expires { get; set; }
        public DateTime? LastModified { get; set; }
        public CacheControl? CacheControl { get; set; }
    }

    public class CacheEtagServices : Service
    {
        public object Any(SetCache request)
        {
            return new HttpResult(request)
            {
                Age = request.Age,
                ETag = request.ETag,
                MaxAge = request.MaxAge,
                Expires = request.Expires,
                LastModified = request.LastModified,
                CacheControl = request.CacheControl.GetValueOrDefault(CacheControl.None),
            };
        }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
        }

        protected void Application_BeginRequest(object src, EventArgs e)
        {
            if (Request.IsLocal)
                Profiler.Start();
        }

        protected void Application_EndRequest(object src, EventArgs e)
        {
            Profiler.Stop();
        }
    }

    public static class HtmlHelpers
    {
        public static MvcHtmlString DisplayPrice(this HtmlHelper html, decimal price)
        {
            return MvcHtmlString.Create(price == 0
                ? "<span>FREE!</span>"
                : $"{price:C2}");
        }
    }
}