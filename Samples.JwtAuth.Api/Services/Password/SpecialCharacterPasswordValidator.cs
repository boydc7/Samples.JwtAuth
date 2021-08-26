using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.JwtAuth.Api.Services
{
    public class SpecialCharacterPasswordValidator : IPasswordValidator
    {
        private const int _minRequiredSpecialChars = 2;

        private static readonly HashSet<char> _specialCharacters = new HashSet<char>
                                                                   {
                                                                       ' ',
                                                                       '\r',
                                                                       '\n',
                                                                       '\f',
                                                                       '\t',
                                                                       '.',
                                                                       '~',
                                                                       '@',
                                                                       '!',
                                                                       '#',
                                                                       '$',
                                                                       '%',
                                                                       '^',
                                                                       '&',
                                                                       '*',
                                                                       '(',
                                                                       ')',
                                                                       '_',
                                                                       '-',
                                                                       '+',
                                                                       '=',
                                                                       '`',
                                                                       '{',
                                                                       '[',
                                                                       '}',
                                                                       ']',
                                                                       '|',
                                                                       '\\',
                                                                       '/',
                                                                       ':',
                                                                       ';',
                                                                       '"',
                                                                       '\'',
                                                                       ',',
                                                                       '<',
                                                                       '>',
                                                                       '?'
                                                                   };

        public ValueTask<bool> IsValidAsync(string password, string userId)
        {
            var specialCharCount = 0;

            foreach (var _ in password.Where(c => _specialCharacters.Contains(c)))
            {
                specialCharCount++;

                if (specialCharCount >= _minRequiredSpecialChars)
                {
                    return ValueTask.FromResult(true);
                }
            }

            return ValueTask.FromResult(false);
        }
    }
}
