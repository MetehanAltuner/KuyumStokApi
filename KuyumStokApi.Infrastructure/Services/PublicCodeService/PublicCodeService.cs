using KuyumStokApi.Application.Interfaces.Services;
using System;
using System.Security.Cryptography;
using System.Text;

namespace KuyumStokApi.Infrastructure.Services.PublicCodeService
{
    /// <summary>Crockford Base32 public code üretimi.</summary>
    public sealed class PublicCodeService : IPublicCodeService
    {
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        public string GenerateStockPublicCode(int length = 10)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Public code uzunluğu 0'dan büyük olmalıdır.");

            var buffer = RandomNumberGenerator.GetBytes(length);
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = Alphabet[buffer[i] & 31];
            }

            return new string(chars);
        }

        public string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length);
            foreach (var ch in input.Trim())
            {
                if (ch == '-' || char.IsWhiteSpace(ch))
                    continue;

                var upper = char.ToUpperInvariant(ch);
                upper = upper switch
                {
                    'O' => '0',
                    'I' => '1',
                    'L' => '1',
                    _ => upper
                };

                sb.Append(upper);
            }

            return sb.ToString();
        }

        public bool IsValid(string normalizedCode)
        {
            if (string.IsNullOrWhiteSpace(normalizedCode))
                return false;

            if (normalizedCode.Length != 10)
                return false;

            foreach (var ch in normalizedCode)
            {
                if (!Alphabet.Contains(ch))
                    return false;
            }

            return true;
        }
    }
}
