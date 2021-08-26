using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public partial class AuthDynProvider : IAuthDynProvider
    {
        public async Task<DynUserToken> GetTokenAsync(string userId, string refreshToken)
        {
            var item = await GetItemAsync(string.Concat(DynUserToken.HashPrefix, userId), refreshToken);

            return item == null || item.Count <= 0
                       ? null
                       : new DynUserToken
                         {
                             HashKey = item.GetValueOrDefault(nameof(DynItem.HashKey))?.S,
                             RangeKey = item.GetValueOrDefault(nameof(DynItem.RangeKey))?.S,
                             ParentToken = item.GetValueOrDefault(nameof(DynUserToken.ParentToken))?.S,
                             ExpiresAt = item.GetValueOrDefault(nameof(DynItem.ExpiresAt))?.N.ToLong() ?? 0,
                         };
        }

        public async IAsyncEnumerable<DynUserToken> GetUserTokensAsync(string userId)
        {
            var query = new QueryRequest
                        {
                            TableName = DynItem.ItemTableName,
                            Select = "ALL_ATTRIBUTES",
                            KeyConditionExpression = string.Concat(nameof(DynItem.HashKey), " = :userTkn"),
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                                                        {
                                                            [":userTkn"] = new AttributeValue(string.Concat(DynUserToken.HashPrefix, userId))
                                                        },
                            ScanIndexForward = true
                        };

            await foreach (var recordBatch in QueryAsync(query))
            {
                foreach (var token in recordBatch.Where(r => r is
                                                 {
                                                     Count: > 0
                                                 })
                                                 .Select(r => new DynUserToken
                                                              {
                                                                  HashKey = r.GetValueOrDefault(nameof(DynItem.HashKey))?.S,
                                                                  RangeKey = r.GetValueOrDefault(nameof(DynItem.RangeKey))?.S,
                                                                  ParentToken = r.GetValueOrDefault(nameof(DynUserToken.ParentToken))?.S,
                                                                  ExpiresAt = r.GetValueOrDefault(nameof(DynItem.ExpiresAt))?.N.ToLong() ?? 0,
                                                              }))
                {
                    yield return token;
                }
            }
        }

        public async Task UpsertTokenAsync(DynUserToken token)
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
                                                                          S = token.HashKey
                                                                      }
                                         },
                                         {
                                             nameof(DynItem.RangeKey), new AttributeValue
                                                                       {
                                                                           S = token.RangeKey
                                                                       }
                                         },
                                         {
                                             nameof(DynItem.ExpiresAt), new AttributeValue
                                                                        {
                                                                            N = token.ExpiresAt.ToStringInvariant()
                                                                        }
                                         },
                                     }
                          };

            if (!string.IsNullOrEmpty(token.ParentToken))
            {
                request.Item[nameof(DynUserToken.ParentToken)] = new AttributeValue
                                                                 {
                                                                     S = token.ParentToken
                                                                 };
            }

            await _dynamoDb.PutItemAsync(request);
        }
    }
}
