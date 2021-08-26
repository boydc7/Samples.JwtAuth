using System.Threading.Tasks;
using BCrypt.Net;
using Crypt = BCrypt.Net.BCrypt;

namespace Samples.JwtAuth.Api.Services
{
    public class PriorUsedPasswordValidator : IPasswordValidator
    {
        private const int _usedPasswordsToCheck = 6;

        private readonly IPasswordService _passwordService;

        public PriorUsedPasswordValidator(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        public async ValueTask<bool> IsValidAsync(string password, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return true;
            }

            var checkedPasswords = 0;

            await foreach (var usedPasswordHash in _passwordService.GetUserPasswordsAsync(userId))
            {
                if (Crypt.EnhancedVerify(password, usedPasswordHash, HashType.SHA512))
                {
                    return false;
                }

                checkedPasswords++;

                if (checkedPasswords >= _usedPasswordsToCheck)
                {
                    break;
                }
            }

            return true;
        }
    }
}
