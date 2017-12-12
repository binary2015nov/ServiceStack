using System;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.Admin;
using ServiceStack.Api.Swagger;
using ServiceStack.Auth;
using ServiceStack.Authentication.OpenId;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.ProtoBuf;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests
{
    public class Global : System.Web.HttpApplication
    {
        public class AppHost : AppHostBase
        {
            private bool StartMqHost = false;

            public AppHost() : base("ServiceStack WebHost IntegrationTests", typeof(AppHost).Assembly)
            {
                Config.AdminAuthSecret = Constants.AuthSecret;
                Config.ApiVersion = "2.0.0";
                Config.UseCamelCase = true;
                Config.EmbeddedResourceSources.Add(GetType().Assembly);
                //var onlyEnableFeatures = Feature.All.Remove(Feature.Jsv | Feature.Soap);
                //Config.EnableFeatures = onlyEnableFeatures;       
            }

            protected override void OnBeforeInit()
            {
                //typeof(Authenticate).AddAttributes(new ExcludeAttribute(Feature.Metadata));
                foreach (var pi in typeof(Authenticate).GetPublicProperties())
                {
                    if (pi.Name != "Provider" && pi.Name != "UserName" && pi.Name != "Password")
                    {
                        pi.AddAttributes(new IgnoreDataMemberAttribute());
                    }
                }
            }

            public override void Configure(Funq.Container container)
            {
                IocShared.Configure(this);

                this.PreRequestFilters.Add((req, res) =>
                {
                    req.Items["_DataSetAtPreRequestFilters"] = true;
                });

                this.GlobalRequestFilters.Add((req, res, dto) =>
                {
                    req.Items["_DataSetAtRequestFilters"] = true;

                    if (dto is RequestFilter requestFilter)
                    {
                        res.StatusCode = requestFilter.StatusCode;
                        if (!requestFilter.HeaderName.IsNullOrEmpty())
                        {
                            res.AddHeader(requestFilter.HeaderName, requestFilter.HeaderValue);
                        }
                        res.Close();
                    }

                    if (dto is IRequiresSession secureRequests)
                    {
                        res.ReturnAuthRequired();
                    }
                });
                
                Plugins.Add(new SoapFormat());
                Plugins.Add(new MiniProfilerFeature());

                container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory(
                        "~/App_Data/db.sqlite".MapHostAbsolutePath(),
                        SqliteDialect.Provider)
                    {
                        ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                    });

                container.Register<ICacheClient>(new MemoryCacheClient());
                //container.Register<ICacheClient>(new BasicRedisClientManager());

                ConfigureAuth(container);

                //container.Register<ISessionFactory>(
                //    c => new SessionFactory(c.Resolve<ICacheClient>()));

                var dbFactory = container.Resolve<IDbConnectionFactory>();

                using (var db = dbFactory.Open())
                    db.DropAndCreateTable<Movie>();

                ModelConfig<Movie>.Id(x => x.Title);
                Routes
                    .Add<Movies>("/custom-movies", "GET, OPTIONS")
                    .Add<Movies>("/custom-movies/genres/{Genre}")
                    .Add<Movie>("/custom-movies", "POST,PUT")
                    .Add<Movie>("/custom-movies/{Id}")
                    .Add<MqHostStats>("/mqstats");

                container.Register<IRedisClientsManager>(c => new RedisManagerPool());

                Plugins.Add(new TemplatePagesFeature());

                Plugins.Add(new ValidationFeature());
                Plugins.Add(new SessionFeature());
                Plugins.Add(new ProtoBufFormat());
                Plugins.Add(new RequestLogsFeature
                {
                    //RequestLogger = new RedisRequestLogger(container.Resolve<IRedisClientsManager>())
                    RequestLogger = new CsvRequestLogger(),
                });
                Plugins.Add(new SwaggerFeature
                {
                    //UseBootstrapTheme = true
                    OperationFilter = x => x.Consumes = x.Produces = new[] { MimeTypes.Json, MimeTypes.Xml }.ToList(),
                    RouteSummary =
                    {
                        { "/swaggerexamples", "Swagger Examples Summary" }
                    }
                });
                Plugins.Add(new PostmanFeature());
                Plugins.Add(new CorsFeature());
                Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });
                Plugins.Add(new AdminFeature());

                container.RegisterValidators(typeof(CustomersValidator).Assembly);

                typeof(ResponseStatus)
                    .AddAttributes(new DescriptionAttribute("This is the Response Status!"));

                typeof(ResponseStatus)
                   .GetProperty("Message")
                   .AddAttributes(new DescriptionAttribute("A human friendly error message"));

                if (StartMqHost)
                {
                    var redisManager = new BasicRedisClientManager();
                    var mqHost = new RedisMqServer(redisManager);
                    mqHost.RegisterHandler<Reverse>(ExecuteMessage);
                    mqHost.Start();
                    container.Register((IMessageService)mqHost);
                }
            }

            protected override void OnAfterInit()
            {
                new ResetMoviesService().Post(new ResetMovies());
            }

            //Configure ServiceStack Authentication and CustomUserSession
            private void ConfigureAuth(Funq.Container container)
            {
                Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                    new IAuthProvider[] {
                        new CredentialsAuthProvider(AppSettings),
                        new FacebookAuthProvider(AppSettings),
                        new TwitterAuthProvider(AppSettings),
                        new GoogleOpenIdOAuthProvider(AppSettings),
                        new OpenIdOAuthProvider(AppSettings),
                        new DigestAuthProvider(AppSettings),
                        new BasicAuthProvider(AppSettings),
                    })
                {
                    IncludeRegistrationService = true
                });
                ServiceStack.Auth.RegisterService.AllowUpdates = true;
                container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));

                var authRepo = (OrmLiteAuthRepository)container.Resolve<IAuthRepository>();
                authRepo.DropAndReCreateTables();
                authRepo.CreateUserAuth(new UserAuth
                {
                    UserName = Constants.AdminName,
                    DisplayName = "The Admin User",
                    Email = Constants.AdminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                }, Constants.AdminPassword);
            }
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (Request.IsLocal)
                Profiler.Start();
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            Profiler.Stop();
        }
    }
}