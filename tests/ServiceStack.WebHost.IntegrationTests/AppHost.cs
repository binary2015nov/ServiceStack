using System.Linq;
using System.Runtime.Serialization;
using Funq;
using ServiceStack.Admin;
using ServiceStack.Api.Swagger;
using ServiceStack.Auth;
using ServiceStack.Authentication.OpenId;
using ServiceStack.Caching;
using ServiceStack.Common.Tests;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.ProtoBuf;
using ServiceStack.Redis;
using ServiceStack.Shared.Tests;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests
{
    public class AppHost : AppHostBase
    {
        protected bool StartMqHost = false;

        public AppHost() : base("ServiceStack WebHost IntegrationTests", typeof(AppHost).Assembly)
        {
            JsConfig.EmitCamelCaseNames = true;
            Config.AdminAuthSecret = Constant.AuthSecret;
            Config.ApiVersion = "2.0.0";
            Config.DebugMode = true;
            
            //Show StackTraces for easier debugging
            //var onlyEnableFeatures = Feature.All.Remove(Feature.Jsv | Feature.Soap);
            //Config.EnableFeatures = onlyEnableFeatures;       
        }

        protected override void OnBeforeInit()
        {
            //typeof(Authenticate).AddAttributes(new ExcludeAttribute(Feature.Metadata));
            foreach (var pi in typeof(Authenticate).GetPublicProperties())
            {
                if (pi.Name != "provider" && pi.Name != "UserName" && pi.Name != "Password")
                {
                    pi.AddAttributes(new IgnoreDataMemberAttribute());
                }
            }
        }

        public override void Configure(Container container)
        {
            IocShared.Configure(this);

            this.PreRequestFilters.Add((req, res) =>
            {
                req.Items["_DataSetAtPreRequestFilters"] = true;
            });

            this.GlobalRequestFilters.Add((req, res, dto) =>
            {
                req.Items["_DataSetAtRequestFilters"] = true;

                var requestFilter = dto as RequestFilter;
                if (requestFilter != null)
                {
                    res.StatusCode = requestFilter.StatusCode;
                    if (!requestFilter.HeaderName.IsNullOrEmpty())
                    {
                        res.AddHeader(requestFilter.HeaderName, requestFilter.HeaderValue);
                    }
                    res.Close();
                }

                var secureRequests = dto as IRequiresSession;
                if (secureRequests != null)
                {
                    res.ReturnAuthRequired();
                }
            });
            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(
                    "~/App_Data/db.sqlite".MapHostAbsolutePath(),
                    SqliteDialect.Provider)
                {
                    ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                });

            container.Register<ICacheClient>(new MemoryCacheClient());
            //container.AdminRegister<ICacheClient>(new BasicRedisClientManager());

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
            var resetMovies = this.Container.Resolve<ResetMoviesService>();
            resetMovies.Post(null);
        }

        //Configure ServiceStack Authentication and CustomUserSession
        private void ConfigureAuth(Container container)
        {
            Routes
                .Add<Register>("/register");

            var appSettings = AppSettings;

            Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[] {
                        new CredentialsAuthProvider(appSettings),
                        new FacebookAuthProvider(appSettings),
                        new TwitterAuthProvider(appSettings),
                        new GoogleOpenIdOAuthProvider(appSettings),
                        new OpenIdOAuthProvider(appSettings),
                        new DigestAuthProvider(appSettings),
                        new BasicAuthProvider(appSettings),
                }));

            Plugins.Add(new RegistrationFeature());

            container.Register<IAuthRepository>(c =>
                new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));

            var authRepo = (OrmLiteAuthRepository)container.Resolve<IAuthRepository>();
            if (AppSettings.Get("RecreateTables", true))
                authRepo.DropAndReCreateTables();
            else
                authRepo.InitSchema();
        }

        public override object OnPreExecuteServiceFilter(IService service, object requestDto, IRequest request, IResponse response)
        {
            if (service is IocScopeService)
                service.InjectRequestIntoDependencies(request);

            return requestDto;
        }
    }
}