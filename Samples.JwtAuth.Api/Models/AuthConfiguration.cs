namespace Samples.JwtAuth.Api.Models
{
    public class AuthApiConfiguration
    {
        public AuthApiAuthConfiguration Auth { get; } = new AuthApiAuthConfiguration();
        public AuthApiAwsConfiguration Aws { get; } = new AuthApiAwsConfiguration();
    }

    public class AuthApiAuthConfiguration
    {
        public int JwtExpiresAfterMinutes { get; set; } = 60 * 1; // 1 hours
        public int JwtRefreshTokenExpiresAfterMinutes { get; set; } = 60 * 24 * 25; // 25 days
        public string JwtIssuer { get; set; }
        public string JwtAudience { get; set; }
        public string JwtSecretKey { get; set; }
        public string JwtEncryptKey { get; set; }
    }

    public class AuthApiAwsConfiguration
    {
        public AuthApiAwsDynamoConfiguration Dynamo { get; set; } = new AuthApiAwsDynamoConfiguration();
    }

    public class AuthApiAwsDynamoConfiguration
    {
        public string ServiceUrl { get; set; }
    }
}
