using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.PasswordHasher
{
    public sealed class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordOptions _opt;

        public PasswordHasher(IOptions<PasswordOptions> opt)
        {
            _opt = opt.Value;
        }

        public string GenerateSalt(int size = 16)
        {
            var bytes = new byte[size];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        public string Hash(string password, string saltBase64)
        {
            // SHA-256 + (salt || password || pepper) + iterasyon
            var salt = Convert.FromBase64String(saltBase64);
            var pepperBytes = Encoding.UTF8.GetBytes(_opt.Pepper ?? string.Empty);

            // İlk birleşim
            var input = Combine(salt, Encoding.UTF8.GetBytes(password), pepperBytes);

            // Iterative hashing
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(input);
            for (int i = 1; i < _opt.Iterations; i++)
                hash = sha.ComputeHash(hash);

            return Convert.ToBase64String(hash);
        }

        public bool Verify(string password, string saltBase64, string expectedHashBase64)
        {
            var computed = Hash(password, saltBase64);
            // Constant-time karşılaştırma
            var a = Convert.FromBase64String(computed);
            var b = Convert.FromBase64String(expectedHashBase64);
            return FixedTimeEquals(a, b);
        }

        private static byte[] Combine(params byte[][] arrays)
        {
            var len = arrays.Sum(a => a.Length);
            var result = new byte[len];
            int pos = 0;
            foreach (var arr in arrays)
            {
                Buffer.BlockCopy(arr, 0, result, pos, arr.Length);
                pos += arr.Length;
            }
            return result;
        }

        private static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
