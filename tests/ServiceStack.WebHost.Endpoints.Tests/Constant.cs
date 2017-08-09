using System;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class Constant
    {
        public static readonly string ServiceStackBaseUri = Environment.GetEnvironmentVariable("CI_BASEURI") ?? "http://localhost:20000";
        public static readonly string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public static readonly string ListeningOn = ServiceStackBaseUri + "/";
        public static readonly string RabbitMQConnString = Environment.GetEnvironmentVariable("CI_RABBITMQ") ?? "localhost";
        public static readonly string SqlServerConnString = Environment.GetEnvironmentVariable("CI_SQLSERVER") ?? @"Data Source=(localdb)\ProjectsV13;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        public const string AspNetBaseUri = "http://localhost:50000/";
        public const string AspNetServiceStackBaseUri = AspNetBaseUri + "api";
    }
}