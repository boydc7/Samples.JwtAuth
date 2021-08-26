using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using BCrypt.Net;
using Samples.JwtAuth.Api.Models;
using Crypt = BCrypt.Net.BCrypt;

namespace Samples.JwtAuth.Api.Services
{
    public class DynamoPasswordService : IPasswordService
    {
        private readonly IAuthDynProvider _authDynProvider;

        public DynamoPasswordService(IAuthDynProvider authDynProvider)
        {
            _authDynProvider = authDynProvider;
        }

        public async Task<string> GetPasswordAsync(string userId)
        {
            var currentActivePassword = await _authDynProvider.GetPasswordAsync(userId);

            return currentActivePassword?.HashedSecret;
        }

        public async IAsyncEnumerable<string> GetUserPasswordsAsync(string userId)
        {
            var query = new QueryRequest
                        {
                            TableName = DynItem.ItemTableName,
                            Select = "ALL_ATTRIBUTES",
                            KeyConditionExpression = string.Concat(nameof(DynItem.HashKey), " = :userId",
                                                                   " AND begins_with(", nameof(DynItem.RangeKey), ", :rprefix)"),
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                                                        {
                                                            [":userId"] = new AttributeValue(userId),
                                                            [":rprefix"] = new AttributeValue(DynUserSecret.RangePrefix)
                                                        },
                            ScanIndexForward = false
                        };

            var currentActivePassword = await GetPasswordAsync(userId);

            yield return currentActivePassword;

            await foreach (var recordBatch in _authDynProvider.QueryAsync(query))
            {
                foreach (var secret in recordBatch.Select(b => b.GetValueOrDefault(nameof(DynUserSecret.HashedSecret))?.S)
                                                  .Where(s => !string.IsNullOrEmpty(s)))
                {
                    yield return secret;
                }
            }
        }

        public async Task SetPasswordAsync(string userId, string secret)
        {
            var newSecret = new DynUserSecret
                            {
                                HashKey = userId,
                                RangeKey = string.Concat(DynUserSecret.RangePrefix, "current"),
                                HashedSecret = Crypt.EnhancedHashPassword(secret, HashType.SHA512, workFactor: 15)
                            };

            await ArchiveCurrentSecretAsync(userId);

            await _authDynProvider.UpsertPasswordAsync(newSecret);
        }

        private async ValueTask ArchiveCurrentSecretAsync(string userId)
        {
            var currentActiveSecret = await _authDynProvider.GetPasswordAsync(userId);

            if (currentActiveSecret == null)
            {
                return;
            }

            // Archive it
            currentActiveSecret.RangeKey = string.Concat(DynUserSecret.RangePrefix, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToStringInvariant());
            currentActiveSecret.ExpiresAt = DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds();

            await _authDynProvider.UpsertPasswordAsync(currentActiveSecret);
        }
    }
}
