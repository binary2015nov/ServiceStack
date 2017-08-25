using System;
using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.Host;

namespace ServiceStack.Mvc
{
	public class FunqValidatorFactory : ValidatorFactoryBase
	{
		private readonly ContainerResolveCache funqBuilder = new ContainerResolveCache();

        private Container container;

		public FunqValidatorFactory(Container container)
		{
            this.container = container;
		}

		public override IValidator CreateInstance(Type validatorType)
		{
			return funqBuilder.CreateInstance(container ?? HostContext.Container, validatorType, true) as IValidator;
		}
	}
}