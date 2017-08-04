using System.Collections.Generic;
using ServiceStack.FluentValidation;

namespace ServiceStack.Common.Tests.FluentValidation
{
    public class Person
    {
        public string Firstname { get; set; }
        public string CreditCard { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public double Weight { get; set; }
        public List<Car> Cars { get; set; }
        public List<Car> Favorites { get; set; }
        public string Lastname { get; set; }
    }

    public class Car
    {
        public string Manufacturer { get; set; }
        public int Age { get; set; }
    }

    public class EmptyValidator : AbstractValidator<Person>
    {

    }

    public class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleFor(x => x.Firstname).Matches("asdfj");

            RuleFor(x => x.CreditCard).CreditCard().Length(10).ExclusiveBetween("asdlöfjasdf", "asldfjlöakjdfsadf");

            RuleFor(x => x.Email).EmailAddress();

            RuleFor(x => x.Age).GreaterThan(15).InclusiveBetween(10, 20).LessThan(30);

            RuleFor(x => x.Weight).Equal(53.7);

            RuleFor(x => x.Cars).SetCollectionValidator(new CarValidator());

            RuleFor(x => x.Favorites).NotNull().NotEmpty().WithErrorCode("ShouldNotBeEmpty");

            RuleFor(x => x.Lastname).NotEmpty();
        }
    }

    public class CarValidator : AbstractValidator<Car>
    {
        public CarValidator()
        {
            RuleFor(x => x.Age).LessThanOrEqualTo(20).NotEqual(100);
            RuleFor(x => x.Manufacturer).Must(m => m == "BMW");
        }
    }

    internal class DtoARequestValidator : AbstractValidator<DtoA>
    {
        internal readonly IDtoBValidator dtoBValidator;

        public DtoARequestValidator(IDtoBValidator dtoBValidator)
        {
            this.dtoBValidator = dtoBValidator;
            RuleFor(dto => dto.FieldA).NotEmpty();
            RuleFor(dto => dto.Items).SetCollectionValidator(dtoBValidator);
        }
    }

    internal class DtoBValidator : AbstractValidator<DtoB>, IDtoBValidator
    {
        public DtoBValidator()
        {
            RuleFor(dto => dto.FieldB).NotEmpty();
        }
    }

    public class DtoA : IReturn<DtoAResponse>
    {
        public string FieldA { get; set; }
        public List<DtoB> Items { get; set; }
    }

    public class DtoAResponse
    {
        public string FieldA { get; set; }
        public List<DtoB> Items { get; set; }
    }

    public class DtoB
    {
        public string FieldB { get; set; }
    }

    internal interface IDtoBValidator : IValidator<DtoB> { }

    public class DtoAService : Service
    {
        public object Any(DtoA request)
        {
            return request.ConvertTo<DtoAResponse>();
        }
    }
}