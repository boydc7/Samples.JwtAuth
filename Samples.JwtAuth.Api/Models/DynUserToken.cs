using Amazon.DynamoDBv2.DataModel;

namespace Samples.JwtAuth.Api.Models
{
    public class DynUserToken : DynItem
    {
        public const string HashPrefix = "dut:";

        public string ParentToken { get; set; }

        [DynamoDBIgnore]
        public string Token => RangeKey;
    }
}
