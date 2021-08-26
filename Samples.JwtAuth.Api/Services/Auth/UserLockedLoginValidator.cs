using System.Threading.Tasks;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public class UserLockedLoginValidator : IUserLoginValidator
    {
        public ValueTask<bool> CanLoginAsync(User user, string userSecret)
            => ValueTask.FromResult(user is
            {
                IsLocked: false
            });
    }
}
