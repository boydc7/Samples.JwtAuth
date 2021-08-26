using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Samples.JwtAuth.Api.Models;
using Samples.JwtAuth.Api.Services;

namespace Samples.JwtAuth.Api.Controllers
{
    [ApiController]
    [Authorize]
    [ApiExplorerSettings(GroupName = "auth")]
    [Route("")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticateService _authenticateService;
        private readonly IUserTokenService _userTokenService;
        private readonly AuthApiAuthConfiguration _authConfiguration;

        public AuthenticationController(IAuthenticateService authenticateService,
                                        IUserTokenService userTokenService,
                                        AuthApiAuthConfiguration authConfiguration)
        {
            _authenticateService = authenticateService;
            _userTokenService = userTokenService;
            _authConfiguration = authConfiguration;
        }

        /// <summary>
        ///     Authenticates a <see cref="AuthenticateRequest" /> and returns a valid JWT if successful
        /// </summary>
        /// <remarks>
        ///     Sample request:
        ///
        ///         POST /login
        ///         {
        ///             "email": "myemail@mydomain.com",
        ///             "secret": "Some Super Secret Password (Or Secret, Or Phrase, Or Other Thing)..."
        ///         }
        ///
        /// </remarks>
        /// <param name="request">The <see cref="AuthenticateRequest" /> definition</param>
        /// <returns>A valid JWT and refresh token in the form of a <see cref="AuthenticateResponse" /> model if successfull</returns>
        /// <response code="200">The JWT for the authenticated user</response>
        /// <response code="400">If the request is invalid (will include additional validation error information)</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        public async Task<ActionResult<AuthenticateResponse>> PostLogin([FromBody] AuthenticateRequest request)
        {
            Request.Cookies.TryGetValue("smprt", out var existingRefreshToken);

            var response = await _authenticateService.AuthenticateAsync(request, existingRefreshToken);

            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        /// <summary>
        ///     Resets the password (and user lock status) for an existing user - currently will succeed if and only if you can provide the correct existing password
        ///     Note that if a user is locked and resets a password, though it unlocks the user, you may still have to wait the required 30 minutes before being
        ///     able to successfully login (typipcally an admin reset could override this, but it is not implemented)
        /// </summary>
        /// <remarks>
        ///     Sample request:
        ///
        ///         POST /reset
        ///         {
        ///             "email": "myemail@mydomain.com",
        ///             "existingSecret": "Some Super Secret Password (Or Secret, Or Phrase, Or Other Thing)...",
        ///             "newSecret": "Some Super Secret Password (Or Secret, Or Phrase, Or Other Thing)..."
        ///         }
        ///
        /// </remarks>
        /// <param name="request">The <see cref="ReauthenticateRequest" /> definition</param>
        /// <returns>A valid JWT and refresh token in the form of a <see cref="AuthenticateResponse" /> model if successfull</returns>
        /// <response code="200">The JWT for the authenticated user</response>
        /// <response code="400">If the request is invalid (will include additional validation error information)</response>
        [HttpPost("reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        public async Task<ActionResult<AuthenticateResponse>> PostReset([FromBody] ReauthenticateRequest request)
        {
            var response = await _authenticateService.ReRegisterUserAsync(request);

            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        /// <summary>
        ///     Logout an authenticated user
        /// </summary>
        /// <remarks>
        ///     Sample request:
        ///
        ///         GET /logout
        ///
        /// </remarks>
        /// <returns>NoContent (204)</returns>
        [HttpGet("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AllowAnonymous]
        public async Task<NoContentResult> GetLogout([FromQuery] bool everywhere = false)
        {
            var userId = User.GetPrimaryUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return NoContent();
            }

            if (everywhere)
            {
                await _userTokenService.DeleteAllUserRefreshTokensAsync(userId);
            }
            else
            {
                Request.Cookies.TryGetValue("smprt", out var existingRefreshToken);

                if (!string.IsNullOrEmpty(existingRefreshToken))
                {
                    await _userTokenService.DeleteUserRefreshTokenAsync(userId, existingRefreshToken);
                }
            }

            SetRefreshTokenCookie(string.Empty);

            return NoContent();
        }

        /// <summary>
        ///     Creates a new user with the email/secret/etc. (from the <see cref="SignupRequest" /> model) and returns a valid JWT and refresh token if successful
        /// </summary>
        /// <remarks>
        ///     Sample request:
        ///
        ///         POST /signup
        ///         {
        ///             "email": "myemail@mydomain.com",
        ///             "secret": "Some Super Secret Password (Or Secret, Or Phrase, Or Other Thing)..."
        ///         }
        ///
        /// </remarks>
        /// <param name="request">The <see cref="SignupRequest" /> definition</param>
        /// <returns>A valid JWT and refresh token in the form of a <see cref="AuthenticateResponse" /> model if successfull</returns>
        /// <response code="200">The JWT for the authenticated user</response>
        /// <response code="400">If the request is invalid (will include additional validation error information)</response>
        [HttpPost("signup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        public async Task<ActionResult<AuthenticateResponse>> PostSignup([FromBody] SignupRequest request)
        {
            var response = await _authenticateService.RegisterUserAsync(request);

            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        /// <summary>
        ///     Refreshes an auth token with an existing refresh token while generating an updated refresh token for use, if the refresh token is valid
        /// </summary>
        /// <remarks>
        ///     Sample request:
        ///
        ///         POST /refresh-token
        ///         {
        ///             "token": "an existing refresh token"
        ///         }
        ///
        /// </remarks>
        /// <param name="request">The optional <see cref="RefreshTokenRequest" /> definition</param>
        /// <returns>A valid JWT and refresh token in the form of a <see cref="AuthenticateResponse" /> model if successfull</returns>
        /// <response code="200">The JWT for the authenticated user</response>
        /// <response code="400">If the request is invalid (will include additional validation error information)</response>
        [HttpPost("refresh-token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        public async Task<ActionResult<AuthenticateResponse>> PostRefreshToken(RefreshTokenRequest request)
        {
            var token = request.Token;

            if (string.IsNullOrEmpty(token))
            {
                if (!Request.Cookies.TryGetValue("smprt", out var xCookieToken))
                {
                    throw new BadRequestException("Invalid or missing token");
                }

                token = xCookieToken;
            }

            var response = await _authenticateService.RefreshTokenAsync(token);

            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        /// <summary>
        ///     Revokes an existing refresh token
        /// </summary>
        /// <remarks>
        ///     Sample request:
        ///
        ///         POST /revoke-token
        ///         {
        ///             "token": "an existing refresh token"
        ///         }
        ///
        /// </remarks>
        /// <param name="request">The optional <see cref="RevokeTokenRequest" /> definition</param>
        /// <returns>HTTP 204 NoContent</returns>
        /// <response code="204">NoContent</response>
        [HttpPost("revoke-token")]
        public async Task<NoContentResult> RevokeToken([FromBody] RevokeTokenRequest request)
        {
            var token = request.Token;

            if (string.IsNullOrEmpty(token))
            {
                if (!Request.Cookies.TryGetValue("smprt", out var ct) || string.IsNullOrEmpty(ct))
                {
                    return NoContent();
                }

                token = ct;
            }

            var userId = User.GetPrimaryUserId();

            await _authenticateService.DeleteRefreshTokenAsync(token, userId);

            return NoContent();
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            Response.Cookies.Append("smprt", refreshToken, new CookieOptions
                                                           {
                                                               HttpOnly = true,
                                                               Expires = DateTimeOffset.UtcNow.AddMinutes(_authConfiguration.JwtRefreshTokenExpiresAfterMinutes.Gz(60 * 24 * 25)),
#if !DEBUG
                                                                        Secure = true
#endif
                                                           });
        }
    }
}
