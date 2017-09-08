using System;
using System.IO;
using ServiceStack.Web;

namespace ServiceStack.Client
{
    public class HtmlServiceClient : ServiceClientBase
    {
        public override string Format => "html";

        public HtmlServiceClient() { }

        public HtmlServiceClient(string baseUri)
        {
            SetBaseUri(baseUri);
        }

        public HtmlServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
            : base(syncReplyBaseUri, asyncOneWayBaseUri) { }

        public override string Accept
        {
            get { return MimeTypes.Html; }
        }

        public override string ContentType
        {
            // Only used by the base class when POST-ing.
            get { return MimeTypes.FormUrlEncoded; }
        }

        public override void SerializeToStream(IRequest request, object requestDto, Stream stream)
        {
            var queryString = QueryStringSerializer.SerializeToString(requestDto);
            stream.Write(queryString);
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            return (T)DeserializeDtoFromHtml(typeof(T), stream);
        }

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get { return DeserializeDtoFromHtml; }
        }

        private object DeserializeDtoFromHtml(Type type, Stream fromStream)
        {
            // TODO: No tests currently use the response, but this could be something that will come in handy later.
            // It isn't trivial though, will have to parse the HTML content.
            //return Activator.CreateInstance(type);
            throw new NotSupportedException($"The response content type is {MimeTypes.Html}, web browser dependency.");
        }
    }
}
