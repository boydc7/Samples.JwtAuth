using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.JwtAuth.Api.Services
{
    public class NumericCharacterPasswordValidator : IPasswordValidator
    {
        private const int _minRequiredNumericChars = 2;

        private static readonly HashSet<char> _numericChars = new HashSet<char>
                                                              {
                                                                  '0',
                                                                  '1',
                                                                  '2',
                                                                  '3',
                                                                  '4',
                                                                  '5',
                                                                  '6',
                                                                  '7',
                                                                  '8',
                                                                  '9'
                                                              };

        public ValueTask<bool> IsValidAsync(string password, string userId)
        {
            var numericCharCount = 0;

            foreach (var _ in password.Where(c => _numericChars.Contains(c)))
            {
                numericCharCount++;

                if (numericCharCount >= _minRequiredNumericChars)
                {
                    return ValueTask.FromResult(true);
                }
            }

            return ValueTask.FromResult(false);
        }
    }
}
