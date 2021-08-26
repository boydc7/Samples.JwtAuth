using FluentValidation;
using Samples.JwtAuth.Api.Models;
using Samples.JwtAuth.Api.Services;

namespace Samples.JwtAuth.Api.Validations
{
    public class SignupRequestValidator : AbstractValidator<SignupRequest>
    {
        public SignupRequestValidator(IPasswordValidationService passwordValidator)
        {
            RuleFor(e => e.Email)
                .NotEmpty()
                .MinimumLength(4)
                .MaximumLength(300)
                .EmailAddress();

            RuleFor(e => e.Secret)
                .NotEmpty()
                .MinimumLength(10)
                .MaximumLength(500);

            RuleFor(e => e.Secret)
                .SetValidator(new IsValidPasswordValidator(passwordValidator));
        }
    }
}
