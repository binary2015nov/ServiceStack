using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Host;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [DataContract]
    public class AlwaysThrows
    {
        [DataMember]
        public int? StatusCode { get; set; }
        [DataMember]
        public string Value { get; set; }
    }

    [Route("/throwslist/{StatusCode}/{Value}")]
    [DataContract]
    public class AlwaysThrowsList
    {
        [DataMember]
        public int? StatusCode { get; set; }
        [DataMember]
        public string Value { get; set; }
    }

    [Route("/throwsvalidation")]
    [DataContract]
    public class AlwaysThrowsValidation
    {
        [DataMember]
        public string Value { get; set; }
    }

    public class AlwaysThrowsValidator : AbstractValidator<AlwaysThrowsValidation>
    {
        public AlwaysThrowsValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }

    public class ThrowArgumentException : IReturn<ThrowArgumentException>
    {
        public int Id { get; set; }
    }

    [DataContract]
    public class AlwaysThrowsResponse : IHasResponseStatus
    {
        public AlwaysThrowsResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        [DataMember]
        public string Result { get; set; }

        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class BasicAuthRequired
    {
        public string Name { get; set; }
    }

    public class AlwaysThrowsService : Service
    {
        public object Any(AlwaysThrows request)
        {
            if (request.StatusCode.HasValue)
            {
                throw new HttpError(
                    request.StatusCode.Value,
                    typeof(NotImplementedException).Name,
                    GetErrorMessage(request.Value));
            }

            throw new NotImplementedException(GetErrorMessage(request.Value));
        }

        public List<AlwaysThrows> Any(AlwaysThrowsList request)
        {
            Any(request.ConvertTo<AlwaysThrows>());

            return new List<AlwaysThrows>();
        }

        public List<AlwaysThrows> Any(AlwaysThrowsValidation request)
        {
            return new List<AlwaysThrows>();
        }

        public static string GetErrorMessage(string value)
        {
            return value + " is not implemented";
        }

        public object Any(BasicAuthRequired request)
        {
            return request;
        }

        public object Any(ThrowArgumentException request)
        {
            throw new ArgumentNullException("Id");
        }
    }

    public class AlwaysThrowsAppHost : AppHostHttpListenerBase
    {
        public AlwaysThrowsAppHost() : base("Always Throws Service", typeof(AlwaysThrowsService).GetAssembly()) { }

        public override void Configure(Container container)
        {
#if !NETCORE
            Plugins.Add(new SoapFormat());
#endif
            Plugins.Add(new ValidationFeature());
            Plugins.Add(new CustomAuthenticationPlugin());
        }
    }

    public class CustomAuthenticationPlugin : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.PreRequestFilters.Add((httpReq, httpResp) =>
            {
                if (httpReq.OperationName != typeof(BasicAuthRequired).Name)
                    return;

                var credentials = httpReq.GetBasicAuthUserAndPassword();
                if (!credentials.HasValue)
                {
                    // throw new UnauthorizedAccessException();
                    httpResp.StatusCode = (int)HttpStatusCode.Unauthorized;
                    httpResp.EndRequestWithNoContent();
                    return;
                }

                string username = credentials.Value.Key;
                string password = credentials.Value.Value;

                // TODO: get DI working
                // TODO: Use PasswordList.SystemPass in production
                var isAuth = password == "p@55word";
                if (!isAuth)
                {
                    // throw new UnauthorizedAccessException();
                    httpResp.StatusCode = (int)HttpStatusCode.Unauthorized;
                    httpResp.EndRequestWithNoContent();
                }
            });
        }
    }

    /// <summary>
    /// This base class executes all Web Services ignorant of the endpoints its hosted on.
    /// The same tests below are re-used by the Unit and Integration TestFixture's declared below
    /// </summary>
    [TestFixture]
    public abstract class WebServicesTests
    {
        public static string ListeningOn = Config.ListeningOn;

        private const string TestString = "ServiceStack";

        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AlwaysThrowsAppHost()
                .Init()
                .Start(ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        protected abstract IServiceClient CreateNewServiceClient();

        [Test]
        public void Can_Handle_Exception_from_AlwaysThrowService()
        {
            var client = CreateNewServiceClient();
            try
            {
                var response = client.Send<AlwaysThrowsResponse>(
                    new AlwaysThrows { Value = TestString });

                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                var responseDto = (AlwaysThrowsResponse)webEx.ResponseDto;

                Assert.That(responseDto.ResponseStatus.ErrorCode,
                    Is.EqualTo(typeof(NotImplementedException).Name));
                Assert.That(responseDto.ResponseStatus.Message,
                    Is.EqualTo(AlwaysThrowsService.GetErrorMessage(TestString)));
            }
        }

        [Test]
        public void Can_Handle_Http_Error_from_AlwaysThrowService()
        {
            var client = CreateNewServiceClient();
            try
            {
                var response = client.Send<AlwaysThrowsResponse>(
                    new AlwaysThrows { StatusCode = 406, Value = TestString });

                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                var responseDto = (AlwaysThrowsResponse)webEx.ResponseDto;

                Assert.That(webEx.StatusCode, Is.EqualTo(406));
                Assert.That(responseDto.ResponseStatus.ErrorCode,
                    Is.EqualTo(typeof(NotImplementedException).Name));
                Assert.That(responseDto.ResponseStatus.Message,
                    Is.EqualTo(AlwaysThrowsService.GetErrorMessage(TestString)));
            }
        }

        [Test]
        public void Can_Handle_ThrowArgumentException()
        {
            var client = CreateNewServiceClient();
            try
            {
                var response = client.Send<ThrowArgumentException>(
                    new ThrowArgumentException());

                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(400));
                Assert.That(webEx.StatusDescription, Is.EqualTo(typeof(ArgumentNullException).Name));
                Assert.That(webEx.ErrorCode, Is.EqualTo(typeof(ArgumentNullException).Name));
            }
        }

        [Test]
        public void Can_Handle_Exception_from_AlwaysThrowsList_with_GET_route()
        {
            var client = CreateNewServiceClient();
#if !NETCORE
            if (client is WcfServiceClient)
                return;
#endif
            try
            {
                var response = client.Get<List<AlwaysThrows>>("/throwslist/404/{0}".Fmt(TestString));

                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(404));

                var responseDto = (ErrorResponse)webEx.ResponseDto;
                Assert.That(responseDto.ResponseStatus.ErrorCode,
                    Is.EqualTo(typeof(NotImplementedException).Name));
                Assert.That(responseDto.ResponseStatus.Message,
                    Is.EqualTo(AlwaysThrowsService.GetErrorMessage(TestString)));
            }
        }

        [Test]
        public void Can_Handle_Exception_from_AlwaysThrowsValidation()
        {
            var client = CreateNewServiceClient();
            try
            {
                var response = client.Send<List<AlwaysThrows>>(
                    new AlwaysThrowsValidation());

                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                var responseDto = (ErrorResponse)webEx.ResponseDto;
                Assert.That(responseDto.ResponseStatus.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(responseDto.ResponseStatus.Message, Is.EqualTo("'Value' should not be empty."));
                Assert.That(responseDto.ResponseStatus.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(responseDto.ResponseStatus.Errors[0].FieldName, Is.EqualTo("Value"));
                Assert.That(responseDto.ResponseStatus.Errors[0].Message, Is.EqualTo("'Value' should not be empty."));
            }
        }

        [Test]
        public void Can_handle_no_content_BasicAuth_exception()
        {
            var client = CreateNewServiceClient();
            try
            {
                var response = client.Send<BasicAuthRequired>(
                    new BasicAuthRequired { Name = "Test" });

                Assert.Fail("Should throw HTTP errors");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Assert.That(webEx.ResponseDto, Is.Null);
            }
        }
    }

    public class XmlIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new XmlServiceClient(ListeningOn);
        }
    }

    public class JsonIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new JsonServiceClient(ListeningOn);
        }
    }

    public class JsonHttpIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new JsonHttpClient(ListeningOn);
        }
    }

    public class JsvIntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new JsvServiceClient(ListeningOn);
        }
    }

#if !NETCORE
    public class Soap11IntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new Soap11ServiceClient(ListeningOn);
        }
    }

    public class Soap12IntegrationTests : WebServicesTests
    {
        protected override IServiceClient CreateNewServiceClient()
        {
            return new Soap12ServiceClient(ListeningOn);
        }
    }
#endif
}