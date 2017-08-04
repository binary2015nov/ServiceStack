using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.FluentValidation;

namespace ServiceStack.Common.Tests.FluentValidation
{
    [TestFixture]
    public class UserSeverityTests
    {
        [Test]
        public void Stores_user_severity_against_validation_failure()
        {
            var validator = new EmptyValidator();
            validator.RuleFor(x => x.Lastname).NotNull().WithSeverity(Severity.Info);
            var result = validator.Validate(new Person());
            Assert.AreEqual(Severity.Info, result.Errors.Single().Severity);
        }

        [Test]
        public void Defaults_user_severity_to_error()
        {
            var validator = new EmptyValidator();
            validator.RuleFor(x => x.Lastname).NotNull();
            var result = validator.Validate(new Person());
            Assert.AreEqual(Severity.Error, result.Errors.Single().Severity);
        }     
    }


}