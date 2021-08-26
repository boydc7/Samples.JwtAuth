using System.Threading.Tasks;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public interface IUserLoginValidationService
    {
        ValueTask<bool> ValidateLoginAsync(User user, string userSecret);
    }
}
