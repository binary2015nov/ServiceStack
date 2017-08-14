using System.Linq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Testing;
using ServiceStack.Validation;

namespace ServiceStack.Common.Tests.FluentValidation
{
    [TestFixture]
    public class ValidationTests
    {
        [Test]
        public void Can_register_IDtoBValidator_separately()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureAppHost = host => {
                    host.RegisterService<DtoAService>();
                    host.Plugins.Add(new ValidationFeature());
                },
                ConfigureContainer = c => {
                    c.RegisterAs<DtoBValidator, IDtoBValidator>();
                    c.RegisterValidators(typeof(DtoARequestValidator).GetAssembly());
                }
            }.Init())
            {
                var dtoAValidator = (DtoARequestValidator)appHost.TryResolve<IValidator<DtoA>>();
                Assert.That(dtoAValidator, Is.Not.Null);
                Assert.That(dtoAValidator.dtoBValidator, Is.Not.Null);
                Assert.That(appHost.TryResolve<IValidator<DtoB>>(), Is.Not.Null);
                Assert.That(appHost.TryResolve<IDtoBValidator>(), Is.Not.Null);

                var result = dtoAValidator.Validate(new DtoA());
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors.Count, Is.EqualTo(1));

                result = dtoAValidator.Validate(new DtoA { FieldA = "foo", Items = new[] { new DtoB() }.ToList() });
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors.Count, Is.EqualTo(1));

                result = dtoAValidator.Validate(new DtoA { FieldA = "foo", Items = new[] { new DtoB { FieldB = "bar" } }.ToList() });
                Assert.That(result.IsValid, Is.True);
            }
        }
    }
}
