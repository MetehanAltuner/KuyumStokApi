using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Security
{
    public sealed class PasswordValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new();
        public int Score { get; set; } // 0-4 arası basit skor (bilgi amaçlı)
    }

    /// <summary>
    /// Register sırasında kullanılacak güçlü parola politikası.
    /// </summary>
    public static class PasswordPolicy
    {
        // Basit ama etkili ortak şifre blok listesi
        private static readonly HashSet<string> Blocklist = new(StringComparer.OrdinalIgnoreCase)
        {
            "123456","1234567","12345678","123456789","1234567890",
            "password","passw0rd","qwerty","111111","000000","abc123",
            "iloveyou","admin","letmein","welcome"
        };

        // En az uzunluk (öneri: 10+)
        private const int MinLength = 10;

        // Şart: küçük, büyük, rakam, sembol
        private static readonly Regex RxLower = new("[a-z]", RegexOptions.Compiled);
        private static readonly Regex RxUpper = new("[A-Z]", RegexOptions.Compiled);
        private static readonly Regex RxDigit = new("\\d", RegexOptions.Compiled);
        private static readonly Regex RxSymbol = new("[^a-zA-Z0-9]", RegexOptions.Compiled);

        // Kullanıcı adı kuralları
        private static readonly Regex RxUsername = new("^[a-zA-Z0-9._-]{3,32}$", RegexOptions.Compiled);
        public static List<string> ValidateUsername(string? username)
        {
            var errs = new List<string>();
            if (string.IsNullOrWhiteSpace(username))
            {
                errs.Add("Kullanıcı adı boş olamaz.");
                return errs;
            }
            if (!RxUsername.IsMatch(username))
                errs.Add("Kullanıcı adı yalnızca harf, rakam, '.', '_' veya '-' içerebilir ve 3-32 karakter olmalıdır.");
            return errs;
        }

        /// <summary>Username format kontrolü (Register’da çağır).</summary>
        public static void EnsureValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("Kullanıcı adı boş olamaz.");

            if (!RxUsername.IsMatch(username))
                throw new InvalidOperationException("Kullanıcı adı yalnızca harf, rakam, '.', '_' veya '-' içerebilir ve 3-32 karakter olmalıdır.");
        }

        /// <summary>Parola kurallarını doğrular; başarısızsa InvalidOperationException atar.</summary>
        public static void EnsureStrong(
            string password,
            string? username = null,
            string? firstName = null,
            string? lastName = null)
        {
            var res = Validate(password, username, firstName, lastName);
            if (!res.IsValid)
                throw new InvalidOperationException(string.Join(" ", res.Errors));
        }

        /// <summary>İstersen hata mesajlarını tek tek görmek için kullan.</summary>
        public static PasswordValidationResult Validate(
            string password,
            string? username = null,
            string? firstName = null,
            string? lastName = null)
        {
            var r = new PasswordValidationResult();

            if (string.IsNullOrEmpty(password))
            {
                r.Errors.Add("Parola boş olamaz.");
                return r;
            }

            if (password.Length < MinLength)
                r.Errors.Add($"Parola en az {MinLength} karakter olmalı.");

            if (password.Any(char.IsWhiteSpace))
                r.Errors.Add("Parola boşluk içeremez.");

            int classes = 0;
            if (RxLower.IsMatch(password)) classes++;
            else r.Errors.Add("En az bir küçük harf içermeli.");

            if (RxUpper.IsMatch(password)) classes++;
            else r.Errors.Add("En az bir büyük harf içermeli.");

            if (RxDigit.IsMatch(password)) classes++;
            else r.Errors.Add("En az bir rakam içermeli.");

            if (RxSymbol.IsMatch(password)) classes++;
            else r.Errors.Add("En az bir sembol içermeli.");

            // tekrar eden karakter zinciri (aaaa, 1111)
            if (HasRepeatedChars(password, 4))
                r.Errors.Add("Aynı karakteri 4+ kez tekrar etmeyin.");

            // ardışık artan/azalan dizi (abcd, 1234)
            if (HasSequentialRun(password, 4))
                r.Errors.Add("4+ ardışık karakter/rakam kullanmayın.");

            // blocklist
            if (Blocklist.Contains(password))
                r.Errors.Add("Çok yaygın bir parola kullanamazsınız.");

            // kişisel bilgi içeremez (basit kontrol)
            var lowers = new[]
            {
                username?.Trim().ToLowerInvariant(),
                firstName?.Trim().ToLowerInvariant(),
                lastName?.Trim().ToLowerInvariant()
            }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            var pLower = password.ToLowerInvariant();
            foreach (var key in lowers)
            {
                if (!string.IsNullOrEmpty(key) && key!.Length >= 3 && pLower.Contains(key))
                {
                    r.Errors.Add("Parola; kullanıcı adı veya ad/soyad içermemelidir.");
                    break;
                }
            }

            // basit skor (bilgi amaçlı)
            r.Score = Math.Min(4, classes + (password.Length >= 14 ? 1 : 0));
            return r;
        }

        private static bool HasRepeatedChars(string s, int threshold)
        {
            var run = 1;
            for (int i = 1; i < s.Length; i++)
            {
                run = s[i] == s[i - 1] ? run + 1 : 1;
                if (run >= threshold) return true;
            }
            return false;
        }

        private static bool HasSequentialRun(string s, int threshold)
        {
            int up = 1, down = 1;

            for (int i = 1; i < s.Length; i++)
            {
                if (s[i] == s[i - 1] + 1) { up++; down = 1; }
                else if (s[i] == s[i - 1] - 1) { down++; up = 1; }
                else { up = down = 1; }

                if (up >= threshold || down >= threshold) return true;
            }
            return false;
        }
    }
}
