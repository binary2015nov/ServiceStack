﻿using System.Collections.Generic;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.TemplateTests
{
    public class QueryProducts : QueryData<Product> {}
    
    public class GetAllProducts : IReturn<GetAllProductsResponse> {}

    public class GetAllProductsResponse
    {
        public Product[] Results { get; set; }
    }

    public class TemplateServiceStackFiltersService : Service
    {
        public object Any(GetAllProducts request) => new GetAllProductsResponse
        {
            Results = TemplateQueryData.Products
        };
    }
    
    public class QueryTemplateRockstars : QueryDb<Rockstar> {}
    
    public class TemplateServiceStackFiltersTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(TemplateIntegrationTests), typeof(MyTemplateServices).GetAssembly())
            {
                Config.DebugMode = true;
            }

            public readonly List<IVirtualPathProvider> TemplateFiles = new List<IVirtualPathProvider> { new MemoryVirtualFiles() };
            public override List<IVirtualPathProvider> GetVirtualFileSources() => TemplateFiles;

            public override void Configure(Container container)
            {
                container.Register<IDbConnectionFactory>(new OrmLiteConnectionFactory(":memory:",
                    SqliteDialect.Provider));

                using (var db = container.Resolve<IDbConnectionFactory>().Open())
                {
                    db.DropAndCreateTable<Rockstar>();
                    db.InsertAll(UnitTestExample.SeedData);
                }

                Plugins.Add(new TemplatePagesFeature
                {
                    Args =
                    {
                        ["products"] = TemplateQueryData.Products,
                    },
                    TemplateFilters = { new TemplateAutoQueryFilters() },
                });
                
                Plugins.Add(new AutoQueryDataFeature { MaxLimit = 100 }
                    .AddDataSource(ctx => ctx.ServiceSource<Product>(ctx.ConvertTo<GetAllProducts>()))
                );
                
                Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });

                var files = TemplateFiles[0];
                
                files.WriteFile("_layout.html", @"
<html>
<body id=root>
{{ page }}
{{ htmlErrorDebug }}
</body>
</html>
");
                files.WriteFile("autoquery-data-products.html", @"
{{ { category, orderBy, take } | withoutNullValues | sendToAutoQuery('QueryProducts') 
   | toResults | select: { it.ProductName }\n }}");

                files.WriteFile("autoquery-rockstars.html", @"
{{ { age, orderBy, take } | withoutNullValues | sendToAutoQuery('QueryTemplateRockstars') 
   | toResults | select: { it.FirstName } { it.LastName }\n }}");
            }
        }

        public static string BaseUrl = Config.ListeningOn;
        
        private readonly ServiceStackHost appHost;
        public TemplateServiceStackFiltersTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(BaseUrl);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Can_call_AutoQuery_Data_services()
        {
            var html = BaseUrl.CombineWith("autoquery-data-products").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Does.StartWith(@"
<html>
<body id=root>

Chai
Chang
Aniseed Syrup".NormalizeNewLines()));
        }

        [Test]
        public void Can_call_AutoQuery_Data_services_with_limit()
        {
            var html = BaseUrl.CombineWith("autoquery-data-products?orderBy=ProductName&take=3").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Does.StartWith(@"
<html>
<body id=root>

Alice Mutton
Aniseed Syrup
Boston Crab Meat


</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Can_call_AutoQuery_Data_services_with_category()
        {
            var html = BaseUrl.CombineWith("autoquery-data-products?category=Beverages").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

Chai
Chang
Guaran&#225; Fant&#225;stica
Sasquatch Ale
Steeleye Stout
C&#244;te de Blaye
Chartreuse verte
Ipoh Coffee
Laughing Lumberjack Lager
Outback Lager
Rh&#246;nbr&#228;u Klosterbier
Lakkalik&#246;&#246;ri


</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Can_call_AutoQuery_Db_services()
        {
            var html = BaseUrl.CombineWith("autoquery-rockstars").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Does.StartWith(@"
<html>
<body id=root>

Jimi Hendrix
Jim Morrison
Kurt Cobain
Elvis Presley
David Grohl
Eddie Vedder
Michael Jackson


</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Can_call_AutoQuery_Db_services_with_limit()
        {
            var html = BaseUrl.CombineWith("autoquery-rockstars?orderBy=FirstName&take=3").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Does.StartWith(@"
<html>
<body id=root>

David Grohl
Eddie Vedder
Elvis Presley


</body>
</html>".NormalizeNewLines()));
        }

        [Test]
        public void Can_call_AutoQuery_Db_services_by_age()
        {
            var html = BaseUrl.CombineWith("autoquery-rockstars?age=27&orderBy=LastName").GetStringFromUrl();
            Assert.That(html.NormalizeNewLines(), Is.EqualTo(@"
<html>
<body id=root>

Kurt Cobain
Jimi Hendrix
Jim Morrison


</body>
</html>".NormalizeNewLines()));
        }

    }
}