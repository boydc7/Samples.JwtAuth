using FluentValidation;
using Samples.JwtAuth.Api.Models;
using Samples.JwtAuth.Api.Services;

namespace Samples.JwtAuth.Api.Validations
{
    public class ReauthenticateRequestValidator : AbstractValidator<ReauthenticateRequest>
    {
        public ReauthenticateRequestValidator(IPasswordValidationService passwordValidator)
        {
            RuleFor(e => e.Email)
                .NotEmpty()
                .MinimumLength(4)
                .MaximumLength(300)
                .EmailAddress();

            RuleFor(e => e.ExistingSecret)
                .NotEmpty()
                .MinimumLength(10)
                .MaximumLength(500);

            RuleFor(e => e.NewSecret)
                .SetValidator(new IsValidPasswordValidator(passwordValidator));
        }
    }
}
