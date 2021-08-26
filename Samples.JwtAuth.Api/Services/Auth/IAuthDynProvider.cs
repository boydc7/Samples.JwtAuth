using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public interface IAuthDynProvider
    {
        Task InitSchemaAsync();
        Task<BatchWriteItemResponse> BatchWriteAsync(BatchWriteItemRequest request);
        Task DeleteItemAsync(string hashKey, string rangeKey);
        IAsyncEnumerable<IReadOnlyList<Dictionary<string, AttributeValue>>> QueryAsync(QueryRequest request);

        Task<DynUser> GetUserAsync(string email);
        Task UpsertUserAsync(DynUser dynUser);

        Task<DynUserSecret> GetPasswordAsync(string email);
        Task UpsertPasswordAsync(DynUserSecret userSecret);

        Task<DynUserToken> GetTokenAsync(string userId, string refreshToken);
        Task UpsertTokenAsync(DynUserToken token);
        IAsyncEnumerable<DynUserToken> GetUserTokensAsync(string userId);
    }
}
