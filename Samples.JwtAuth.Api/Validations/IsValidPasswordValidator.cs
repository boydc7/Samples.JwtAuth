using FluentValidation;
using Samples.JwtAuth.Api.Services;

namespace Samples.JwtAuth.Api.Validations
{
    public class IsValidPasswordValidator : AbstractValidator<string>
    {
        public IsValidPasswordValidator(IPasswordValidationService passwordValidator)
        {
            RuleFor(s => s)
                .NotEmpty();

            RuleFor(e => e.Length)
                .GreaterThan(10)
                .LessThanOrEqualTo(500)
                .Unless(string.IsNullOrEmpty);

            RuleFor(e => e)
                .MustAsync(async (e, _) => await passwordValidator.ValidateAsync(e, null))
                .Unless(string.IsNullOrEmpty)
                .WithMessage("Password must meet complexity requirements.");
        }
    }
}
