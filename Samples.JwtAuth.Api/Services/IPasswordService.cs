using System.Collections.Generic;
using System.Threading.Tasks;

namespace Samples.JwtAuth.Api.Services
{
    public interface IPasswordService
    {
        Task<string> GetPasswordAsync(string userId);
        IAsyncEnumerable<string> GetUserPasswordsAsync(string userId);
        Task SetPasswordAsync(string userId, string secret);
    }
}
