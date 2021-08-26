using System.Threading.Tasks;
using BCrypt.Net;
using Samples.JwtAuth.Api.Models;
using Crypt = BCrypt.Net.BCrypt;

namespace Samples.JwtAuth.Api.Services
{
    public class SecretValidLoginValidator : IUserLoginValidator
    {
        private readonly IPasswordService _passwordService;

        public SecretValidLoginValidator(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        public async ValueTask<bool> CanLoginAsync(User user, string userSecret)
        {
            if (userSecret == null)
            {
                return true;
            }

            if (user?.Email == null)
            {
                return false;
            }

            var existingUserSecretHash = await _passwordService.GetPasswordAsync(user.Email);

            return !string.IsNullOrEmpty(existingUserSecretHash)
                   &&
                   Crypt.EnhancedVerify(userSecret, existingUserSecretHash, HashType.SHA512);
        }
    }
}
