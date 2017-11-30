#if !LITE
using System;
using System.IO;
using System.Xml;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class XmlServiceClient : ServiceClientBase
    {
        public override string Format => "xml";

        public XmlServiceClient() { }

        public XmlServiceClient(string baseUri) 
        {
            SetBaseUri(baseUri);
        }

        public XmlServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
            : base(syncReplyBaseUri, asyncOneWayBaseUri) { }

        public override string ContentType => $"application/{Format}";

        public override void SerializeToStream(IRequest request, object requestDto, Stream stream)
        {
            if (requestDto == null)
                return;

            XmlSerializer.Serialize(requestDto, stream);
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            try
            {
                return XmlSerializer.Deserialize<T>(stream);
            }
            catch (XmlException ex)
            {
                if (ex.LineNumber == 0 && ex.LinePosition == 0)
                {
                    //if (ex.Message == "Unexpected end of file.") //Empty responses
                    return default(T);
                }
                throw;
            }
        }

        public override StreamDeserializerDelegate StreamDeserializer => (t, s) => XmlSerializer.Deserialize(s, t);
    }
}
#endif