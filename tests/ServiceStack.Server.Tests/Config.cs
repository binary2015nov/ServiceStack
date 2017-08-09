using System;

namespace ServiceStack.Server.Tests
{
    public class Config
    {
        public const string ServiceStackBaseUri = "http://localhost:20000";
        public const string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public const string ListeningOn = ServiceStackBaseUri + "/";

        public static readonly string RabbitMQConnString = Environment.GetEnvironmentVariable("CI_RABBITMQ") ?? "localhost";
        public static readonly string SqlServerBuildDb = Environment.GetEnvironmentVariable("CI_SQLSERVER")
            ?? @"Data Source=(localdb)\ProjectsV13;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
    }
}