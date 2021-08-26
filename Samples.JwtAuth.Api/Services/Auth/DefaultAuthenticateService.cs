using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using Samples.JwtAuth.Api.Models;
using Crypt = BCrypt.Net.BCrypt;

namespace Samples.JwtAuth.Api.Services
{
    internal class DefaultAuthenticateService : IAuthenticateService
    {
        private readonly IUserService _userService;
        private readonly IUserTokenService _userTokenService;
        private readonly IUserLoginValidationService _userLoginValidationService;
        private readonly AuthApiAuthConfiguration _authConfiguration;
        private readonly IPasswordService _passwordService;
        private readonly IPasswordValidationService _passwordValidationService;

        public DefaultAuthenticateService(IUserService userService,
                                          IUserTokenService userTokenService,
                                          IUserLoginValidationService userLoginValidationService,
                                          AuthApiAuthConfiguration authConfiguration,
                                          IPasswordService passwordService,
                                          IPasswordValidationService passwordValidationService)
        {
            _userService = userService;
            _userTokenService = userTokenService;
            _userLoginValidationService = userLoginValidationService;
            _authConfiguration = authConfiguration;
            _passwordService = passwordService;
            _passwordValidationService = passwordValidationService;
        }

        public async Task<AuthenticateResponse> AuthenticateAsync(AuthenticateRequest request, string existingRefreshToken)
        {
            var existingUser = await _userService.GetAsync(request.Email);

            if (existingUser == null)
            {
                throw new BadRequestException("Invalid authenticate request");
            }

            var canLogin = await _userLoginValidationService.ValidateLoginAsync(existingUser, request.Secret);

            existingUser.LastAuthAttemptedOn = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!canLogin)
            {
                if (existingUser != null)
                {
                    existingUser.FailedAttempts++;

                    if (existingUser.FailedAttempts >= 6)
                    {
                        await _userService.LockUserAsync(existingUser);
                    }
                    else
                    {
                        await _userService.UpsertAsync(existingUser);
                    }
                }

                throw new BadRequestException("Invalid authenticate request");
            }

            existingUser.IsLocked = false;
            existingUser.FailedAttempts = 0;

            await _userService.UpsertAsync(existingUser);

            var response = await GetValidAuthResponse(existingUser, existingRefreshToken);

            return response;
        }

        public async Task<AuthenticateResponse> RegisterUserAsync(SignupRequest request)
        {
            var existingUser = await _userService.GetAsync(request.Email);

            if (existingUser != null)
            {
                throw new RecordExistsException();
            }

            var nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var newUser = new User
                          {
                              Email = request.Email,
                              SecretLastChangedOnUtc = nowUtc,
                              LastAuthAttemptedOn = nowUtc
                          };

            await _userService.UpsertAsync(newUser);

            await _passwordService.SetPasswordAsync(newUser.Email, request.Secret);

            var response = await GetValidAuthResponse(newUser, null);

            return response;
        }

        public async Task<AuthenticateResponse> ReRegisterUserAsync(ReauthenticateRequest request)
        {
            var existingUser = await _userService.GetAsync(request.Email);

            if (existingUser == null)
            {
                throw new BadRequestException("Invalid reauthenticate request");
            }

            // Revoke all refresh tokens for this user...
            await _userTokenService.DeleteAllUserRefreshTokensAsync(existingUser.Email);

            // Validate the new password should be allowed
            var newPasswordValid = await _passwordValidationService.ValidateAsync(request.NewSecret, existingUser.Email);

            if (!newPasswordValid)
            {
                throw new BadRequestException("Invalid reauthenticate request");
            }

            // Validate existing password matches
            var currentUserHashedSecret = await _passwordService.GetPasswordAsync(existingUser.Email);

            if (!Crypt.EnhancedVerify(request.ExistingSecret, currentUserHashedSecret, HashType.SHA512))
            {
                throw new BadRequestException("Invalid reauthenticate request");
            }

            // Update the user
            var nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            existingUser.SecretLastChangedOnUtc = nowUtc;
            existingUser.LastAuthAttemptedOn = nowUtc;
            existingUser.FailedAttempts = 0;
            existingUser.IsLocked = false;

            await _userService.UpsertAsync(existingUser);

            await _passwordService.SetPasswordAsync(existingUser.Email, request.NewSecret);

            var response = await GetValidAuthResponse(existingUser, null);

            return response;
        }

        public async Task<AuthenticateResponse> RefreshTokenAsync(string fromRefreshToken)
        {
            var tokenUserIdIndex = fromRefreshToken.LastIndexOf('|');

            var tokenUserId = tokenUserIdIndex > 0
                                  ? fromRefreshToken[(tokenUserIdIndex + 1)..]
                                  : null;

            var user = string.IsNullOrEmpty(tokenUserId)
                           ? null
                           : await _userService.GetAsync(tokenUserId);

            var canLogin = user != null && await _userLoginValidationService.ValidateLoginAsync(user, null);

            if (!canLogin)
            {
                // Normally this would be enqueued or something, but...
                // Could typically also only invalidate this particular token's descendents...but, you know...
                await _userTokenService.DeleteAllUserRefreshTokensAsync(user.Email);

                throw new BadRequestException("Invalid or missing refresh token");
            }

            // Ensure the refresh token exists and is valid
            var serverToken = await _userTokenService.GetRefreshTokenAsync(fromRefreshToken, user.Email);

            if (serverToken == null || serverToken.ExpiresOnUtc <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                // Normally this would be enqueued or something, but...
                // Could typically also only invalidate this particular token's descendents...but, you know...
                await _userTokenService.DeleteAllUserRefreshTokensAsync(user.Email);

                throw new BadRequestException("Invalid or missing refresh token (code stxtneuiz)");
            }

            // Valid refresh token, generate new tokens
            var response = await GetValidAuthResponse(user, fromRefreshToken);

            return response;
        }

        private async Task AddRefreshTokenAsync(string refreshToken, string userId, string refreshedFromToken)
        {
            await _userTokenService.AddRefreshTokenAsync(refreshToken, userId, refreshedFromToken);

            // Invalidate the token just refreshed from
            if (!string.IsNullOrEmpty(refreshedFromToken) &&
                !refreshToken.Equals(refreshedFromToken, StringComparison.OrdinalIgnoreCase))
            {
                await DeleteRefreshTokenAsync(refreshedFromToken, userId);
            }
        }

        public async Task DeleteRefreshTokenAsync(string refreshToken, string userId)
        {
            var token = await _userTokenService.GetRefreshTokenAsync(refreshToken, userId);

            if (token == null)
            {
                return;
            }

            await _userTokenService.DeleteUserRefreshTokenAsync(userId, refreshToken);

            // Normally here we'd also delete all decscendents of this token...skipping for now...
        }

        private async Task<AuthenticateResponse> GetValidAuthResponse(User user, string existingRefreshToken)
        {
            var accessToken = GenerateJwtToken(user);
            var refreshToken = await GetRefreshTokenAsync(user.Email, existingRefreshToken);

            var response = new AuthenticateResponse
                           {
                               AccessToken = accessToken,
                               RefreshToken = refreshToken
                           };

            return response;
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_authConfiguration.JwtSecretKey);
            var encryptKey = Encoding.ASCII.GetBytes(_authConfiguration.JwtEncryptKey);

            var tokenId = Guid.NewGuid().ToString("N");

            // Expire after about a day by default
            var expiresIn = TimeSpan.FromMinutes(_authConfiguration.JwtExpiresAfterMinutes.Gz(60));

            var claims = new List<Claim>
                         {
                             new Claim("tokenId", tokenId),
                             new Claim(ClaimTypes.NameIdentifier, user.Email),
                             new Claim(ClaimTypes.Email, user.Email)
                         };

            var tokenDescriptor = new SecurityTokenDescriptor
                                  {
                                      Subject = new ClaimsIdentity(claims),
                                      Audience = _authConfiguration.JwtAudience,
                                      Expires = DateTime.UtcNow.AddMinutes(expiresIn.TotalMinutes),
                                      SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                                      EncryptingCredentials = new EncryptingCredentials(new SymmetricSecurityKey(encryptKey), SecurityAlgorithms.Aes128KW, SecurityAlgorithms.Aes128CbcHmacSha256),
                                      Issuer = _authConfiguration.JwtIssuer
                                  };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            var authToken = tokenHandler.WriteToken(token);

            return authToken;
        }

        private async Task<string> GetRefreshTokenAsync(string email, string refreshedFromToken)
        {
            using var rngProvider = new RNGCryptoServiceProvider();

            var bytes = new byte[100];

            rngProvider.GetBytes(bytes);

            var token = string.Concat(bytes.ToSafeBase64().Left(100), "|", email);

            await AddRefreshTokenAsync(token, email, refreshedFromToken);

            return token;
        }

    }
}
