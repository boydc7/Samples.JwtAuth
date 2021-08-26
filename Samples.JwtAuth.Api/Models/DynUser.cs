using Amazon.DynamoDBv2.DataModel;

namespace Samples.JwtAuth.Api.Models
{
    public class DynUser : DynItem
    {
        public long SecretLastChangedOnUtc { get; set; }
        public bool IsLocked { get; set; }
        public long LastLockedOnUtc { get; set; }
        public long LastAuthAttemptedOn { get; set; }
        public int FailedAttempts { get; set; }

        [DynamoDBIgnore]
        public string Email
        {
            get => RangeKey;
            set => RangeKey = value;
        }
    }
}
