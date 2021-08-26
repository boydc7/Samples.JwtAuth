using System;
using System.Threading.Tasks;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public class DynamoUserService : IUserService
    {
        private readonly IAuthDynProvider _authDynProvider;
        private readonly IUserTokenService _userTokenService;

        public DynamoUserService(IAuthDynProvider authDynProvider, IUserTokenService userTokenService)
        {
            _authDynProvider = authDynProvider;
            _userTokenService = userTokenService;
        }

        public async Task<User> GetAsync(string email)
        {
            var dynUser = await _authDynProvider.GetUserAsync(email);

            return dynUser == null
                       ? null
                       : new User
                         {
                             Email = dynUser.Email,
                             SecretLastChangedOnUtc = dynUser.SecretLastChangedOnUtc,
                             IsLocked = dynUser.IsLocked,
                             LastLockedOnUtc = dynUser.LastLockedOnUtc,
                             LastAuthAttemptedOn = dynUser.LastAuthAttemptedOn,
                             FailedAttempts = dynUser.FailedAttempts
                         };
        }

        public async Task UpsertAsync(User user)
        {
            if (user?.Email == null)
            {
                return;
            }

            var dynUser = await _authDynProvider.GetUserAsync(user.Email)
                          ??
                          new DynUser
                          {
                              Email = user.Email
                          };

            dynUser.SecretLastChangedOnUtc = user.SecretLastChangedOnUtc.Gz(dynUser.SecretLastChangedOnUtc);
            dynUser.LastLockedOnUtc = user.LastLockedOnUtc;
            dynUser.IsLocked = user.IsLocked;
            dynUser.FailedAttempts = user.FailedAttempts;
            dynUser.LastAuthAttemptedOn = user.LastAuthAttemptedOn.Gz(dynUser.LastAuthAttemptedOn);

            await _authDynProvider.UpsertUserAsync(dynUser);
        }

        public async Task LockUserAsync(User user)
        {
            if (user?.Email == null)
            {
                return;
            }

            var dynUser = await _authDynProvider.GetUserAsync(user.Email);

            if (dynUser == null)
            {
                return;
            }

            dynUser.FailedAttempts = user.FailedAttempts.Gz(dynUser.FailedAttempts);
            dynUser.LastAuthAttemptedOn = user.LastAuthAttemptedOn.Gz(dynUser.LastAuthAttemptedOn);
            dynUser.LastLockedOnUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            dynUser.IsLocked = true;

            await _authDynProvider.UpsertUserAsync(dynUser);

            await _userTokenService.DeleteAllUserRefreshTokensAsync(user.Email);
        }
    }
}
