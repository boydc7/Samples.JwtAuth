using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Services
{
    public static class Extensions
    {
        public static string ToStringInvariant<T>(this T source)
            where T : IFormattable
            => source == null
                   ? null
                   : source.ToString(null, CultureInfo.InvariantCulture);

        public static long ToLong(this string value, long defaultValue = 0)
            => string.IsNullOrEmpty(value)
                   ? defaultValue
                   : long.TryParse(value, out var i)
                       ? i
                       : defaultValue;

        public static int ToInt(this string value, int defaultValue = 0)
            => string.IsNullOrEmpty(value)
                   ? defaultValue
                   : int.TryParse(value, out var i)
                       ? i
                       : defaultValue;

        public static long Gz(this long first, long second)
            => first > 0
                   ? first
                   : second;

        public static int Gz(this int first, int second) => first > 0
                                                                ? first
                                                                : second;

        public static string ToSafeBase64(this byte[] bytes) => Convert.ToBase64String(bytes)
                                                                       .Replace('+', '_')
                                                                       .Replace('/', '-')
                                                                       .Replace('=', '~');

        public static string Left(this string source, int length)
            => string.IsNullOrEmpty(source)
                   ? string.Empty
                   : source.Length >= length
                       ? source.Substring(0, length)
                       : source;

        public static string Coalesce(this string first, string second)
            => string.IsNullOrEmpty(first)
                   ? second
                   : first;

        public static string GetPrimaryUserId(this ClaimsPrincipal principal)
        {
            if (principal?.Identity is not ClaimsIdentity claimsIdentity ||
                claimsIdentity?.Claims == null)
            {
                return null;
            }

            var idValue = claimsIdentity.Claims
                                        .SingleOrDefault(c => c.Type.Equals(ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase) &&
                                                              !string.IsNullOrEmpty(c.Value));

            return idValue?.Value;
        }

        public static async IAsyncEnumerable<List<T>> ToBatchesOfAsync<T>(this IAsyncEnumerable<T> source, int batchSize, bool serial = false)
        {
            if (source == null)
            {
                yield break;
            }

            var batch = new List<T>(batchSize);

            await foreach (var item in source)
            {
                batch.Add(item);

                if (batch.Count < batchSize)
                {
                    continue;
                }

                yield return batch;

                if (serial)
                {
                    batch.Clear();
                }
                else
                {
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        public static async Task<bool> WaitForTableToCreateAsync(this IAmazonDynamoDB dynamoDb, string tableName, TimeSpan? timeout = null)
        {
            var start = DateTime.UtcNow;

            timeout ??= TimeSpan.FromMinutes(4);

            do
            {
                try
                {
                    var response = await dynamoDb.DescribeTableAsync(tableName, ApplicationShutdownCancellationSource.Instance.Token);

                    if (response.Table.TableStatus
                                .Value
                                .Equals(nameof(TableStatus.ACTIVE),
                                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if ((DateTime.UtcNow - start) > timeout.Value)
                    {
                        return false;
                    }

                    await Task.Delay(2150);
                }
                catch(AmazonDynamoDBException awsx) when(awsx.ErrorCode.Equals(DynamoErrorCodes.NotFound, StringComparison.OrdinalIgnoreCase))
                {
                    if ((DateTime.UtcNow - start).TotalSeconds > 65)
                    {
                        throw;
                    }

                    // Eventually consitent model, so these can occur in normal operation...
                    await Task.Delay(500);
                }
                catch(ResourceNotFoundException)
                {
                    if ((DateTime.UtcNow - start).TotalSeconds > 65)
                    {
                        throw;
                    }

                    // Eventually consitent model, so these can occur in normal operation...
                    await Task.Delay(500);
                }
            } while (true);
        }
    }
}
