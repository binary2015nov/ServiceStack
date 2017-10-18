using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/hellozip")]
    [DataContract]
    public class HelloZip : IReturn<HelloZipResponse>
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> Test { get; set; }
    }

    [DataContract]
    public class HelloZipResponse
    {
        [DataMember]
        public string Result { get; set; }
    }

    public class HelloZipService : IService
    {
        public object Any(HelloZip request)
        {
            return request.Test == null
                ? new HelloZipResponse { Result = $"Hello, {request.Name}" }
                : new HelloZipResponse { Result = $"Hello, {request.Name} ({request.Test?.Count})" };
        }
    }
}