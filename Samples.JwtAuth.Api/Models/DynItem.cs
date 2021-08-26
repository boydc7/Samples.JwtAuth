using Amazon.DynamoDBv2.DataModel;

namespace Samples.JwtAuth.Api.Models
{
    [DynamoDBTable(ItemTableName)]
    public abstract class DynItem
    {
        public const string ItemTableName = "items";

        [DynamoDBHashKey]
        public string HashKey { get; set; }

        [DynamoDBRangeKey]
        public string RangeKey { get; set; }

        public long ExpiresAt { get; set; }
    }
}
