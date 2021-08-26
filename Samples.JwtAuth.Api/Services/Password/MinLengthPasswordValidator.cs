using System.Threading.Tasks;

namespace Samples.JwtAuth.Api.Services
{
    public class MinLengthPasswordValidator : IPasswordValidator
    {
        private const int _minLengthRequired = 10;

        public ValueTask<bool> IsValidAsync(string password, string userId)
            => ValueTask.FromResult(password.Length >= _minLengthRequired);
    }
}
