using System;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.RabbitMq;
using ServiceStack.Redis;

namespace ServiceStack.Common.Tests.Messaging
{
    [TestFixture]
    public abstract class MqServerAppHostTests
    {
        private const string BaseUri = "http://localhost:56789";
        protected const string ListeningOn = "http://*:56789/";

        protected ServiceStackHost AppHost;
        protected readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(60);

        public abstract IMessageService CreateMqServer(int retryCount = 1);

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            AppHost = new MqTestsAppHost(() => CreateMqServer())
                .Init()
                .Start(ListeningOn);
        }

        [OneTimeTearDown]
        public virtual void TestFixtureTearDown()
        {
            AppHost.Dispose();
        }

        [Test]
        public void Can_Publish_to_AnyTestMq_Service()
        {
            using (var mqFactory = AppHost.TryResolve<IMessageFactory>())
            {
                var request = new AnyTestMq { Id = 1 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                    mqProducer.Publish(request);

                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var msg = mqClient.Get<AnyTestMqResponse>(QueueNames<AnyTestMqResponse>.In, MessageTimeout);
                    mqClient.Ack(msg);
                    Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
                }
            }
        }

        [Test]
        public void Can_Publish_to_AnyTestMqAsync_Service()
        {
            using (var mqFactory = AppHost.TryResolve<IMessageFactory>())
            {
                var request = new AnyTestMqAsync { Id = 1 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                    mqProducer.Publish(request);

                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var msg = mqClient.Get<AnyTestMqResponse>(QueueNames<AnyTestMqResponse>.In, MessageTimeout);
                    mqClient.Ack(msg);
                    Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
                }
            }
        }

        [Test]
        public void Can_Publish_to_PostTestMq_Service()
        {
            using (var mqFactory = AppHost.TryResolve<IMessageFactory>())
            {
                var request = new PostTestMq { Id = 2 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                    mqProducer.Publish(request);

                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var msg = mqClient.Get<PostTestMqResponse>(QueueNames<PostTestMqResponse>.In, MessageTimeout);
                    mqClient.Ack(msg);
                    Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
                }
            }
        }

        [Test]
        public void SendOneWay_calls_AnyTestMq_Service_via_MQ()
        {
            var client = new JsonServiceClient(BaseUri);
            var request = new AnyTestMq { Id = 3 };

            client.SendOneWay(request);

            using (var mqFactory = AppHost.TryResolve<IMessageFactory>())
            using (var mqClient = mqFactory.CreateMessageQueueClient())
            {
                var msg = mqClient.Get<AnyTestMqResponse>(QueueNames<AnyTestMqResponse>.In, MessageTimeout);
                mqClient.Ack(msg);
                Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
            }
        }

        [Test]
        public void SendOneWay_calls_PostTestMq_Service_via_MQ()
        {
            var client = new JsonServiceClient(BaseUri);
            var request = new PostTestMq { Id = 4 };

            client.SendOneWay(request);

            using (var mqFactory = AppHost.TryResolve<IMessageFactory>())
            using (var mqClient = mqFactory.CreateMessageQueueClient())
            {
                var msg = mqClient.Get<PostTestMqResponse>(QueueNames<PostTestMqResponse>.In, MessageTimeout);
                mqClient.Ack(msg);
                Assert.That(msg.GetBody().CorrelationId, Is.EqualTo(request.Id));
            }
        }

        [Test]
        public void Does_execute_validation_filters()
        {
            using (var mqFactory = AppHost.TryResolve<IMessageFactory>())
            {
                var request = new ValidateTestMq { Id = -10 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    mqProducer.Publish(request);

                    var errorMsg = mqClient.Get<ValidateTestMq>(QueueNames<ValidateTestMq>.Dlq, MessageTimeout);
                    mqClient.Ack(errorMsg);

                    Assert.That(errorMsg.Error.ErrorCode, Is.EqualTo("PositiveIntegersOnly"));

                    request = new ValidateTestMq { Id = 10 };
                    mqProducer.Publish(request);
                    var responseMsg = mqClient.Get<ValidateTestMqResponse>(QueueNames<ValidateTestMqResponse>.In, MessageTimeout);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().CorrelationId, Is.EqualTo(request.Id));
                }
            }
        }

        [Test]
        public void Does_handle_generic_errors()
        {
            using (var mqFactory = AppHost.TryResolve<IMessageFactory>())
            {
                var request = new ThrowGenericError { Id = 1 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    mqProducer.Publish(request);

                    var msg = mqClient.Get<ThrowGenericError>(QueueNames<ThrowGenericError>.Dlq, MessageTimeout);
                    mqClient.Ack(msg);

                    Assert.That(msg.Error.ErrorCode, Is.EqualTo("ArgumentException"));
                }
            }
        }

        [Test]
        public void Does_execute_ReplyTo_validation_filters()
        {
            using (var mqFactory = AppHost.TryResolve<IMessageFactory>())
            {
                var request = new ValidateTestMq { Id = -10 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var requestMsg = new Message<ValidateTestMq>(request)
                    {
                        ReplyTo = "mq:{0}.replyto".Fmt(request.GetType().Name)
                    };
                    mqProducer.Publish(requestMsg);

                    var errorMsg = mqClient.Get<ValidateTestMqResponse>(requestMsg.ReplyTo, MessageTimeout);
                    mqClient.Ack(errorMsg);

                    Assert.That(errorMsg.GetBody().ResponseStatus.ErrorCode, Is.EqualTo("PositiveIntegersOnly"));

                    request = new ValidateTestMq { Id = 10 };
                    requestMsg = new Message<ValidateTestMq>(request)
                    {
                        ReplyTo = "mq:{0}.replyto".Fmt(request.GetType().Name)
                    };
                    mqProducer.Publish(requestMsg);
                    var responseMsg = mqClient.Get<ValidateTestMqResponse>(requestMsg.ReplyTo, MessageTimeout);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().CorrelationId, Is.EqualTo(request.Id));
                }
            }
        }

        [Test]
        public void Does_handle_ReplyTo_generic_errors()
        {
            using (var mqFactory = AppHost.TryResolve<IMessageFactory>())
            {
                var request = new ThrowGenericError { Id = 1 };

                using (var mqProducer = mqFactory.CreateMessageProducer())
                using (var mqClient = mqFactory.CreateMessageQueueClient())
                {
                    var requestMsg = new Message<ThrowGenericError>(request)
                    {
                        ReplyTo = "mq:{0}.replyto".Fmt(request.GetType().Name)
                    };
                    mqProducer.Publish(requestMsg);

                    var msg = mqClient.Get<ErrorResponse>(requestMsg.ReplyTo, MessageTimeout);
                    mqClient.Ack(msg);

                    Assert.That(msg.GetBody().ResponseStatus.ErrorCode, Is.EqualTo("ArgumentException"));
                }
            }
        }
    }

    [TestFixture, Explicit]
    public class RedisMqServerAppHostTests : MqServerAppHostTests
    {
        public RedisMqServerAppHostTests()
        {
            using (var redis = ((RedisMqServer)CreateMqServer()).ClientsManager.GetClient())
                redis.FlushAll();
        }

        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new RedisMqServer(new PooledRedisClientManager()) { RetryCount = retryCount };
        }
    }

    [TestFixture, Explicit]
    public class RabbitMqServerAppHostTests : MqServerAppHostTests
    {
        public RabbitMqServerAppHostTests()
        {
            using (var conn = ((RabbitMqServer)CreateMqServer()).ConnectionFactory.CreateConnection())
            using (var channel = conn.CreateModel())
            {
                channel.PurgeQueue<AnyTestMq>();
                channel.PurgeQueue<AnyTestMqAsync>();
                channel.PurgeQueue<AnyTestMqResponse>();
                channel.PurgeQueue<PostTestMq>();
                channel.PurgeQueue<PostTestMqResponse>();
                channel.PurgeQueue<ValidateTestMq>();
                channel.PurgeQueue<ValidateTestMqResponse>();
                channel.PurgeQueue<ThrowGenericError>();
            }
        }

        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new RabbitMqServer { RetryCount = 1 };
        }
    }

    [TestFixture]
    public class InMemoryMqServerAppHostTests : MqServerAppHostTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new InMemoryTransientMessageService { RetryCount = retryCount };
        }
    }
}