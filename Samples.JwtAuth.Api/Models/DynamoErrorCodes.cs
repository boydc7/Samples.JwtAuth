namespace Samples.JwtAuth.Api.Models
{
    public class DynamoErrorCodes
    {
        public const string NotFound = "ResourceNotFoundException";
        public const string AlreadyExists = "ResourceInUseException";
        public const string ConditionalCheckFailedException = "ConditionalCheckFailedException";
        public const string ConditionalCheckFailed = "ConditionalCheckFailed";
    }
}
