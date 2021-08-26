using System.Collections.Generic;
using System.Threading.Tasks;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public class CompositeUserLoginValidationService : IUserLoginValidationService
    {
        private readonly IEnumerable<IUserLoginValidator> _loginValidators;

        public CompositeUserLoginValidationService(IEnumerable<IUserLoginValidator> loginValidators)
        {
            _loginValidators = loginValidators;
        }

        public async ValueTask<bool> ValidateLoginAsync(User user, string userSecret)
        {
            foreach (var loginValidator in _loginValidators)
            {
                var result = await loginValidator.CanLoginAsync(user, userSecret);

                if (!result)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
