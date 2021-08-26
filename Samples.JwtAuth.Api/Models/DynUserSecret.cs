namespace Samples.JwtAuth.Api.Models
{
    public class DynUserSecret : DynItem
    {
        public const string RangePrefix = "dus:";

        public string HashedSecret { get; set; }
    }
}
