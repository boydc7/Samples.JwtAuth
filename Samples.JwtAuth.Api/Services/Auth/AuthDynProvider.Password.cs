using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public partial class AuthDynProvider : IAuthDynProvider
    {
        public async Task<DynUserSecret> GetPasswordAsync(string email)
        {
            var item = await GetItemAsync(email, string.Concat(DynUserSecret.RangePrefix, "current"));

            return item == null || item.Count <= 0
                       ? null
                       : new DynUserSecret
                         {
                             HashKey = item.GetValueOrDefault(nameof(DynItem.HashKey))?.S,
                             RangeKey = item.GetValueOrDefault(nameof(DynItem.RangeKey))?.S,
                             HashedSecret = item.GetValueOrDefault(nameof(DynUserSecret.HashedSecret))?.S
                         };
        }

        public async Task UpsertPasswordAsync(DynUserSecret userSecret)
        {
            var request = new PutItemRequest
                          {
                              TableName = DynItem.ItemTableName,
                              ReturnValues = ReturnValue.NONE,
                              Item = new Dictionary<string, AttributeValue>
                                     {
                                         {
                                             nameof(DynItem.HashKey), new AttributeValue
                                                                      {
                                                                          S = userSecret.HashKey
                                                                      }
                                         },
                                         {
                                             nameof(DynItem.RangeKey), new AttributeValue
                                                                       {
                                                                           S = userSecret.RangeKey
                                                                       }
                                         },
                                         {
                                             nameof(DynItem.ExpiresAt), new AttributeValue
                                                                        {
                                                                            N = userSecret.ExpiresAt.ToStringInvariant()
                                                                        }
                                         },
                                         {
                                             nameof(DynUserSecret.HashedSecret), new AttributeValue
                                                                                 {
                                                                                     S = userSecret.HashedSecret
                                                                                 }
                                         }
                                     }
                          };

            await _dynamoDb.PutItemAsync(request);
        }
    }
}
