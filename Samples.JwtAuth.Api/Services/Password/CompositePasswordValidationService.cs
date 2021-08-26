using System.Collections.Generic;
using System.Threading.Tasks;

namespace Samples.JwtAuth.Api.Services
{
    public class CompositePasswordValidationService : IPasswordValidationService
    {
        private readonly IEnumerable<IPasswordValidator> _passwordValidators;

        public CompositePasswordValidationService(IEnumerable<IPasswordValidator> passwordValidators)
        {
            _passwordValidators = passwordValidators;
        }

        public async ValueTask<bool> ValidateAsync(string password, string userId)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            foreach (var passwordValidator in _passwordValidators)
            {
                var result = await passwordValidator.IsValidAsync(password, userId);

                if (!result)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
