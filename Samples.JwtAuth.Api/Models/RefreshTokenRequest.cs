namespace Samples.JwtAuth.Api.Models
{
    public class RefreshTokenRequest
    {
        public string Token { get; set; }
    }

    public class RevokeTokenRequest
    {
        public string Token { get; set; }
    }
}
