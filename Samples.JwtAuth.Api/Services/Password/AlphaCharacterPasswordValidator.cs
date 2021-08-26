using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.JwtAuth.Api.Services
{
    public class AlphaCharacterPasswordValidator : IPasswordValidator
    {
        private const int _minRequiredAlphaChars = 2;

        private static readonly HashSet<char> _alphaChars = new HashSet<char>
                                                            {
                                                                'a',
                                                                'b',
                                                                'c',
                                                                'd',
                                                                'e',
                                                                'f',
                                                                'g',
                                                                'h',
                                                                'i',
                                                                'j',
                                                                'k',
                                                                'l',
                                                                'm',
                                                                'n',
                                                                'o',
                                                                'p',
                                                                'q',
                                                                'r',
                                                                's',
                                                                't',
                                                                'u',
                                                                'v',
                                                                'w',
                                                                'x',
                                                                'y',
                                                                'z',
                                                                'A',
                                                                'B',
                                                                'C',
                                                                'D',
                                                                'E',
                                                                'F',
                                                                'G',
                                                                'H',
                                                                'I',
                                                                'J',
                                                                'K',
                                                                'L',
                                                                'M',
                                                                'N',
                                                                'O',
                                                                'P',
                                                                'Q',
                                                                'R',
                                                                'S',
                                                                'T',
                                                                'U',
                                                                'V',
                                                                'W',
                                                                'X',
                                                                'Y',
                                                                'Z'
                                                            };

        public ValueTask<bool> IsValidAsync(string password, string userId)
        {
            var alphaCharCount = 0;

            foreach (var _ in password.Where(c => _alphaChars.Contains(c)))
            {
                alphaCharCount++;

                if (alphaCharCount >= _minRequiredAlphaChars)
                {
                    return ValueTask.FromResult(true);
                }
            }

            return ValueTask.FromResult(false);
        }
    }
}
