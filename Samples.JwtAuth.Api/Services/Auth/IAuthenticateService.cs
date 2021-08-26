using System.Threading.Tasks;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public interface IAuthenticateService
    {
        Task<AuthenticateResponse> AuthenticateAsync(AuthenticateRequest request, string existingRefreshToken);
        Task<AuthenticateResponse> RegisterUserAsync(SignupRequest request);
        Task<AuthenticateResponse> RefreshTokenAsync(string fromRefreshToken);
        Task DeleteRefreshTokenAsync(string refreshToken, string userId);
        Task<AuthenticateResponse> ReRegisterUserAsync(ReauthenticateRequest request);
    }
}
