using System.Threading.Tasks;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public interface IUserTokenService
    {
        Task AddRefreshTokenAsync(string refreshToken, string userId, string parentRefreshToken);
        Task<AuthRefreshToken> GetRefreshTokenAsync(string refreshToken, string userId);
        Task DeleteAllUserRefreshTokensAsync(string userId);
        Task DeleteUserRefreshTokenAsync(string userId, string refreshToken);
    }
}
