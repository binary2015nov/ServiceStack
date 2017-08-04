using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;

namespace ServiceStack.Common.Tests.FluentValidation
{
    [TestFixture]
    public class ErrorCodeTests
    {
        public ValidationResult Result { get; set; }

        [OneTimeSetUp]
        public void SetUp()
        {
            var person = new Person
            {
                Firstname = "max",
                CreditCard = "1asdf2",
                Email = "email",
                Age = 27,
                Weight = 53.7,
                Cars = new List<Car> {
                    new Car { Manufacturer = "Audi", Age = 100 }
                }
            };

            var validator = new PersonValidator();
            Result = validator.Validate(person);
        }

        [Test]
        public void Firstname()
        {
            Assert.AreEqual(1, Result.Errors.Count(f => f.PropertyName == "Firstname"));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "Firstname" && f.ErrorCode == ValidationErrors.RegularExpression));
        }

        [Test]
        public void CreditCard()
        {
            Assert.AreEqual(3, Result.Errors.Count(f => f.PropertyName == "CreditCard"));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "CreditCard" && f.ErrorCode == ValidationErrors.CreditCard));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "CreditCard" && f.ErrorCode == ValidationErrors.Length &&
                f.FormattedMessagePlaceholderValues.ContainsKey("MinLength")));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "CreditCard" && f.ErrorCode == ValidationErrors.ExclusiveBetween));
        }

        [Test]
        public void Email()
        {
            Assert.AreEqual(1, Result.Errors.Count(f => f.PropertyName == "Email"));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "Email" && f.ErrorCode == ValidationErrors.Email));
        }

        [Test]
        public void Age()
        {
            Assert.AreEqual(1, Result.Errors.Count(f => f.PropertyName == "Age"));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "Age" && f.ErrorCode == ValidationErrors.InclusiveBetween));
        }

        [Test]
        public void Weight()
        {
            Assert.AreEqual(0, Result.Errors.Count(f => f.PropertyName == "Weight"));
        }

        [Test]
        public void LengthContainsPlaceholders()
        {
            Assert.IsTrue(Result.Errors.Where(f => f.ErrorCode == ValidationErrors.Length).Any(f => f.FormattedMessagePlaceholderValues.ContainsKey("MinLength")));
        }

        [Test]
        public void Cars()
        {
            Assert.AreEqual(2, Result.Errors.Count(f => f.PropertyName == "Cars[0].Age"));
            Assert.AreEqual(1, Result.Errors.Count(f => f.PropertyName == "Cars[0].Manufacturer"));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "Cars[0].Age" && f.ErrorCode == ValidationErrors.LessThanOrEqual));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "Cars[0].Age" && f.ErrorCode == ValidationErrors.NotEqual));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "Cars[0].Manufacturer" && f.ErrorCode == ValidationErrors.Predicate));
        }

        [Test]
        public void Favorites()
        {
            Assert.AreEqual(2, Result.Errors.Count(f => f.PropertyName == "Favorites"));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "Favorites" && f.ErrorCode == "ShouldNotBeEmpty"));
        }

        [Test]
        public void Lastname()
        {
            Assert.AreEqual(1, Result.Errors.Count(f => f.PropertyName == "Lastname"));
            Assert.IsTrue(Result.Errors.Any(f => f.PropertyName == "Lastname" && f.ErrorCode == ValidationErrors.NotEmpty));
        }
    }
}
