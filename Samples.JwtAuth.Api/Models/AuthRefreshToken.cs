namespace Samples.JwtAuth.Api.Models
{
    public class AuthRefreshToken
    {
        public string Token { get; set; }
        public string UserId { get; set; }
        public string ParentToken { get; set; }

        public long ExpiresOnUtc { get; set; }
    }
}
