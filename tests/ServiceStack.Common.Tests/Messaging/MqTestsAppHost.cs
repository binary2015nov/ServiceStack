using System;
using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.Messaging;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests.Messaging
{
    public class MqTestsAppHost : AppHostHttpListenerBase
    {
        private readonly Func<IMessageService> createMqServerFn;

        public MqTestsAppHost(Func<IMessageService> createMqServerFn)
            : base(typeof(MqTestsAppHost).Name, typeof(AnyTestMq).GetAssembly())
        {
            this.createMqServerFn = createMqServerFn;
        }

        public override void Configure(Container container)
        {
            Plugins.Add(new ValidationFeature());
            container.RegisterValidators(typeof(ValidateTestMqValidator).GetAssembly());

            container.Register(c => createMqServerFn());

            var mqServer = container.Resolve<IMessageService>();
            mqServer.RegisterHandler<AnyTestMq>(ExecuteMessage);
            mqServer.RegisterHandler<AnyTestMqAsync>(ExecuteMessage);
            mqServer.RegisterHandler<PostTestMq>(ExecuteMessage);
            mqServer.RegisterHandler<ValidateTestMq>(ExecuteMessage);
            mqServer.RegisterHandler<ThrowGenericError>(ExecuteMessage);

            mqServer.Start();
        }
    }

    public class ValidateTestMqValidator : AbstractValidator<ValidateTestMq>
    {
        public ValidateTestMqValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("PositiveIntegersOnly");
        }
    }
}
