using System;
using System.Threading.Tasks;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public class UserLockedRecentlyLoginValidator : IUserLoginValidator
    {
        private const long _lockPeriodMinimumSeconds = 60 * 30; // 30 mins

        public ValueTask<bool> CanLoginAsync(User user, string userSecret)
        {
            if (user == null)
            {
                return ValueTask.FromResult(false);
            }

            if (user.LastLockedOnUtc <= 0)
            {
                return ValueTask.FromResult(true);
            }

            var lockedAgoSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - user.LastLockedOnUtc;

            return ValueTask.FromResult(lockedAgoSeconds > _lockPeriodMinimumSeconds);
        }
    }
}
