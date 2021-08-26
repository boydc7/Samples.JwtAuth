using System.Threading.Tasks;

namespace Samples.JwtAuth.Api.Services
{
    public interface IPasswordValidator
    {
        ValueTask<bool> IsValidAsync(string password, string userId);
    }

    public interface IPasswordValidationService
    {
        ValueTask<bool> ValidateAsync(string password, string userId);
    }
}
