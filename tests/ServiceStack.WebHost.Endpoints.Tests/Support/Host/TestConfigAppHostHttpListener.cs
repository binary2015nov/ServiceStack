using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Host
{
	[DataContract]
	[Route("/login/{UserName}/{Password}")]
	public class BclDto
	{
		[DataMember(Name = "uname")]
		public string UserName { get; set; }

		[DataMember(Name = "pwd")]
		public string Password { get; set; }
	}

	[DataContract]
	public class BclDtoResponse
	{
		[DataMember(Name = "uname")]
		public string UserName { get; set; }

		[DataMember(Name = "pwd")]
		public string Password { get; set; }
	}

	public class BclDtoService : Service
	{
		public object Any(BclDto request)
		{
			return new BclDtoResponse { UserName = request.UserName, Password = request.Password };
		}
	}

    public class TestConfigAppHostHttpListener : AppHostHttpListenerBase
    {
        public TestConfigAppHostHttpListener() : base("TestConfigAppHost Service", typeof(BclDtoService).Assembly)
        {
            Config.UseBclJsonSerializers = true;
        }

        public override void Configure(Funq.Container container) { }
    }
}