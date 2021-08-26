using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public partial class AuthDynProvider : IAuthDynProvider
    {
        private readonly IAmazonDynamoDB _dynamoDb;

        public AuthDynProvider(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb ?? throw new ArgumentNullException(nameof(dynamoDb));
        }

        public async Task InitSchemaAsync()
        {
            var tableRequest = new CreateTableRequest
                               {
                                   TableName = DynItem.ItemTableName,
                                   KeySchema = new List<KeySchemaElement>
                                               {
                                                   new KeySchemaElement(nameof(DynItem.HashKey), KeyType.HASH),
                                                   new KeySchemaElement(nameof(DynItem.RangeKey), KeyType.RANGE),
                                               },
                                   AttributeDefinitions = new List<AttributeDefinition>
                                                          {
                                                              new AttributeDefinition(nameof(DynItem.HashKey), ScalarAttributeType.S),
                                                              new AttributeDefinition(nameof(DynItem.RangeKey), ScalarAttributeType.S)
                                                          },
                                   BillingMode = BillingMode.PAY_PER_REQUEST,
                                   ProvisionedThroughput = null,
                                   SSESpecification = new SSESpecification
                                                      {
                                                          Enabled = true,
                                                          SSEType = SSEType.AES256
                                                      }
                               };

            // Create brick table
            try
            {
                await _dynamoDb.CreateTableAsync(tableRequest, ApplicationShutdownCancellationSource.Instance.Token);
            }
            catch(ResourceInUseException)
            {
                // Already exists, continue along...
            }

            await _dynamoDb.WaitForTableToCreateAsync(DynItem.ItemTableName);

            // Set the ttl
            var ttlBsDesribed = await _dynamoDb.DescribeTimeToLiveAsync(DynItem.ItemTableName);

            if (ttlBsDesribed.TimeToLiveDescription.TimeToLiveStatus.Value.Equals(TimeToLiveStatus.DISABLED, StringComparison.OrdinalIgnoreCase))
            {
                await _dynamoDb.UpdateTimeToLiveAsync(new UpdateTimeToLiveRequest
                                                      {
                                                          TableName = DynItem.ItemTableName,
                                                          TimeToLiveSpecification = new TimeToLiveSpecification
                                                                                    {
                                                                                        AttributeName = nameof(DynItem.ExpiresAt),
                                                                                        Enabled = true
                                                                                    }
                                                      });
            }
        }

        private async Task<Dictionary<string, AttributeValue>> GetItemAsync(string hash, string range)
        {
            var getRequest = new GetItemRequest
                             {
                                 TableName = DynItem.ItemTableName,
                                 Key = new Dictionary<string, AttributeValue>
                                       {
                                           {
                                               nameof(DynItem.HashKey), new AttributeValue
                                                                        {
                                                                            S = hash
                                                                        }
                                           },
                                           {
                                               nameof(DynItem.RangeKey), new AttributeValue
                                                                         {
                                                                             S = range
                                                                         }
                                           }
                                       }
                             };

            var result = await _dynamoDb.GetItemAsync(getRequest);

            return result?.Item;
        }

        public async Task<BatchWriteItemResponse> BatchWriteAsync(BatchWriteItemRequest request)
        {
            var response = await _dynamoDb.BatchWriteItemAsync(request);

            return response;
        }

        public async Task DeleteItemAsync(string hashKey, string rangeKey)
        {
            var deleteRequest = new DeleteItemRequest
                                {
                                    TableName = DynItem.ItemTableName,
                                    Key = new Dictionary<string, AttributeValue>
                                          {
                                              {
                                                  nameof(DynItem.HashKey), new AttributeValue
                                                                           {
                                                                               S = hashKey
                                                                           }
                                              },
                                              {
                                                  nameof(DynItem.RangeKey), new AttributeValue
                                                                            {
                                                                                S = rangeKey
                                                                            }
                                              }
                                          }
                                };

            await _dynamoDb.DeleteItemAsync(deleteRequest);
        }

        public async IAsyncEnumerable<IReadOnlyList<Dictionary<string, AttributeValue>>> QueryAsync(QueryRequest request)
        {
            QueryResponse response = null;

            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }

                response = await _dynamoDb.QueryAsync(request);

                if ((response?.Items) == null || response.Items.Count <= 0)
                {
                    yield break;
                }

                yield return response.Items;

            } while (response.LastEvaluatedKey != null && response.LastEvaluatedKey.Count > 0);
        }
    }
}
