using System.Threading.Tasks;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public interface IUserLoginValidator
    {
        ValueTask<bool> CanLoginAsync(User user, string userSecret);
    }
}
