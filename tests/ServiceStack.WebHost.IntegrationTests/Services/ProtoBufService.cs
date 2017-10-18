using System.Runtime.Serialization;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [DataContract]
    public class ProtoBufEmail
    {
        [DataMember(Order = 1)]
        public string ToAddress { get; set; }
        [DataMember(Order = 2)]
        public string FromAddress { get; set; }
        [DataMember(Order = 3)]
        public string Subject { get; set; }
        [DataMember(Order = 4)]
        public string Body { get; set; }
        [DataMember(Order = 5)]
        public byte[] AttachmentData { get; set; }
    }

    [DataContract]
    public class ProtoBufEmailResponse
    {
        [DataMember(Order = 1)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class ProtoBufEmailService : Service
    {
        public object Any(ProtoBufEmail request)
        {
            return request;
        }
    }
}