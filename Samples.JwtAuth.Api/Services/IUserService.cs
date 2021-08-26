using System.Threading.Tasks;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public interface IUserService
    {
        Task<User> GetAsync(string userId);
        Task UpsertAsync(User user);
        Task LockUserAsync(User user);
    }
}
