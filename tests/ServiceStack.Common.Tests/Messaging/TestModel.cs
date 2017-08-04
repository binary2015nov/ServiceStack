namespace ServiceStack.Common.Tests.Messaging
{
    public class AnyTestMq
    {
        public int Id { get; set; }
    }

    public class AnyTestMqAsync
    {
        public int Id { get; set; }
    }

    public class AnyTestMqResponse
    {
        public int CorrelationId { get; set; }
    }

    public class PostTestMq
    {
        public int Id { get; set; }
    }

    public class PostTestMqResponse
    {
        public int CorrelationId { get; set; }
    }

    public class ValidateTestMq
    {
        public int Id { get; set; }
    }

    public class ValidateTestMqResponse
    {
        public int CorrelationId { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class ThrowGenericError
    {
        public int Id { get; set; }
    }

    public class HelloRabbit
    {
        public string Name { get; set; }
    }

    //Dummy messages to delete Queue's created else where.
    public class AlwaysThrows { }
    public class Hello { }
    public class HelloResponse { }
    public class Reverse { }
    public class Rot13 { }
    public class Wait { }
}
