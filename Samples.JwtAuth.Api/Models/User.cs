namespace Samples.JwtAuth.Api.Models
{
    public class User
    {
        public string Email { get; set; }

        public long SecretLastChangedOnUtc { get; set; }
        public bool IsLocked { get; set; }
        public long LastLockedOnUtc { get; set; }
        public long LastAuthAttemptedOn { get; set; }
        public int FailedAttempts { get; set; }
    }
}
