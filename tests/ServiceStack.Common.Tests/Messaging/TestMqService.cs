using System;
using System.Threading.Tasks;

namespace ServiceStack.Common.Tests.Messaging
{
    public class TestMqService : Service
    {
        public object Any(AnyTestMq request)
        {
            return new AnyTestMqResponse { CorrelationId = request.Id };
        }

        public Task<object> Any(AnyTestMqAsync request)
        {
            return  Task.Factory.StartNew(() =>
               new AnyTestMqResponse { CorrelationId = request.Id } as object);
        }

        public object Post(PostTestMq request)
        {
            return new PostTestMqResponse { CorrelationId = request.Id };
        }

        public object Post(ValidateTestMq request)
        {
            return new ValidateTestMqResponse { CorrelationId = request.Id };
        }

        public object Post(ThrowGenericError request)
        {
            throw new ArgumentException("request");
        }
    }
}
