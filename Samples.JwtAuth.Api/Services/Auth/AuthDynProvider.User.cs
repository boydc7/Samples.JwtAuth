using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public partial class AuthDynProvider : IAuthDynProvider
    {
        public async Task<DynUser> GetUserAsync(string email)
        {
            var item = await GetItemAsync(string.Concat("u:", email), email);

            return item == null || item.Count <= 0
                       ? null
                       : new DynUser
                         {
                             HashKey = item.GetValueOrDefault(nameof(DynItem.HashKey))?.S,
                             RangeKey = item.GetValueOrDefault(nameof(DynItem.RangeKey))?.S,
                             SecretLastChangedOnUtc = item.GetValueOrDefault(nameof(DynUser.SecretLastChangedOnUtc))?.N.ToLong() ?? 0,
                             IsLocked = item.GetValueOrDefault(nameof(DynUser.IsLocked))?.BOOL ?? false,
                             LastLockedOnUtc = item.GetValueOrDefault(nameof(DynUser.LastLockedOnUtc))?.N.ToLong() ?? 0,
                             LastAuthAttemptedOn = item.GetValueOrDefault(nameof(DynUser.LastAuthAttemptedOn))?.N.ToLong() ?? 0,
                             FailedAttempts = item.GetValueOrDefault(nameof(DynUser.FailedAttempts))?.N.ToInt() ?? 0,
                         };
        }

        public async Task UpsertUserAsync(DynUser dynUser)
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
                                                                          S = string.Concat("u:", dynUser.Email)
                                                                      }
                                         },
                                         {
                                             nameof(DynItem.RangeKey), new AttributeValue
                                                                       {
                                                                           S = dynUser.Email
                                                                       }
                                         },
                                         {
                                             nameof(DynUser.SecretLastChangedOnUtc), new AttributeValue
                                                                                     {
                                                                                         N = dynUser.SecretLastChangedOnUtc.ToStringInvariant()
                                                                                     }
                                         },
                                         {
                                             nameof(DynUser.LastLockedOnUtc), new AttributeValue
                                                                              {
                                                                                  N = dynUser.LastLockedOnUtc.ToStringInvariant()
                                                                              }
                                         },
                                         {
                                             nameof(DynUser.IsLocked), new AttributeValue
                                                                       {
                                                                           BOOL = dynUser.IsLocked
                                                                       }
                                         },
                                         {
                                             nameof(DynUser.LastAuthAttemptedOn), new AttributeValue
                                                                                  {
                                                                                      N = dynUser.LastAuthAttemptedOn.ToStringInvariant()
                                                                                  }
                                         },
                                         {
                                             nameof(DynUser.FailedAttempts), new AttributeValue
                                                                             {
                                                                                 N = dynUser.FailedAttempts.ToStringInvariant()
                                                                             }
                                         },
                                     }
                          };

            await _dynamoDb.PutItemAsync(request);
        }
    }
}
