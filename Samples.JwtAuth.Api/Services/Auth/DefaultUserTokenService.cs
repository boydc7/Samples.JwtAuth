using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public class DefaultUserTokenService : IUserTokenService
    {
        private readonly IAuthDynProvider _authDynProvider;
        private readonly TimeSpan _refreshTokenExpiry;

        public DefaultUserTokenService(IAuthDynProvider authDynProvider,
                                       AuthApiAuthConfiguration authConfiguration)
        {
            _authDynProvider = authDynProvider;

            _refreshTokenExpiry = TimeSpan.FromMinutes(authConfiguration.JwtRefreshTokenExpiresAfterMinutes.Gz(60 * 24 * 25));
        }

        public async Task<AuthRefreshToken> GetRefreshTokenAsync(string refreshToken, string userId)
        {
            var dynToken = await _authDynProvider.GetTokenAsync(refreshToken, userId);

            return dynToken == null
                       ? null
                       : new AuthRefreshToken
                         {
                             Token = dynToken.Token,
                             UserId = userId,
                             ExpiresOnUtc = dynToken.ExpiresAt,
                             ParentToken = dynToken.ParentToken
                         };
        }

        public Task AddRefreshTokenAsync(string refreshToken, string userId, string parentRefreshToken)
            => _authDynProvider.UpsertTokenAsync(new DynUserToken
                                                 {
                                                     HashKey = string.Concat(DynUserToken.HashPrefix, userId),
                                                     RangeKey = refreshToken,
                                                     ParentToken = parentRefreshToken,
                                                     ExpiresAt = DateTimeOffset.UtcNow.Add(_refreshTokenExpiry).ToUnixTimeSeconds()
                                                 });

        public Task DeleteUserRefreshTokenAsync(string userId, string refreshToken)
            => _authDynProvider.DeleteItemAsync(string.Concat(DynUserToken.HashPrefix, userId), refreshToken);

        public async Task DeleteAllUserRefreshTokensAsync(string userId)
        {
            await foreach (var tokenBatch in _authDynProvider.GetUserTokensAsync(userId)
                                                             .ToBatchesOfAsync(25))
            {
                var deleteRequests = tokenBatch.Select(t => new WriteRequest(new DeleteRequest(new Dictionary<string, AttributeValue>
                                                                                               {
                                                                                                   [nameof(DynItem.HashKey)] = new AttributeValue(t.HashKey),
                                                                                                   [nameof(DynItem.RangeKey)] = new AttributeValue(t.RangeKey)
                                                                                               })))
                                               .ToList();

                await _authDynProvider.BatchWriteAsync(new BatchWriteItemRequest
                                                       {
                                                           ReturnConsumedCapacity = ReturnConsumedCapacity.NONE,
                                                           ReturnItemCollectionMetrics = ReturnItemCollectionMetrics.NONE,
                                                           RequestItems = new Dictionary<string, List<WriteRequest>>
                                                                          {
                                                                              [DynItem.ItemTableName] = deleteRequests
                                                                          }
                                                       });
            }
        }
    }
}