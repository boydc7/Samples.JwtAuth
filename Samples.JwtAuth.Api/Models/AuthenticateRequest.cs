using System.ComponentModel.DataAnnotations;

namespace Samples.JwtAuth.Api.Models
{
    public class AuthenticateRequest
    {
        [Required]
        [MinLength(1)]
        public string Email { get; set; }

        [Required]
        [MinLength(10)]
        [MaxLength(500)]
        public string Secret { get; set; }
    }

    public class ReauthenticateRequest
    {
        [Required]
        [MinLength(1)]
        public string Email { get; set; }

        [Required]
        [MinLength(10)]
        [MaxLength(500)]
        public string ExistingSecret { get; set; }

        [Required]
        [MinLength(10)]
        [MaxLength(500)]
        public string NewSecret { get; set; }
    }

    public class AuthenticateResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
