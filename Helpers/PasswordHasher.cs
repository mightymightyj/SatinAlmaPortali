using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace satinalmaportali2.Helpers {
    public static class PasswordHasher {
        private const string Salt = "SatinAlmaPortali_Secured_Salt_V1_2026";

        /// <summary>
        /// Şifreyi standart SHA-256 + Salt ile hash'ler (Base64).
        /// </summary>
        public static string HashPassword(string password) {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password + Salt);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Girilen şifreyi veritabanındaki kayıt ile doğrular.
        /// Standart Salted SHA-256, Raw SHA-256 (Base64/Hex), Düz Metin (Plaintext),
        /// MD5, SHA-1 ve boşluk temizleme (Trim) varyasyonlarını destekler.
        /// </summary>
        public static bool VerifyPassword(string password, string? storedHash) {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(password)) return false;
            
            var stored = storedHash.Trim();
            var pass = password;
            var passTrimmed = password.Trim();

            // 1. Standart Salted SHA-256 (Base64) - Tam Eşleşme
            string saltedHash = HashPassword(pass);
            if (string.Equals(stored, saltedHash, StringComparison.Ordinal)) return true;

            string saltedHashTrimmed = HashPassword(passTrimmed);
            if (string.Equals(stored, saltedHashTrimmed, StringComparison.Ordinal)) return true;

            // 2. Düz Metin (Plaintext / Legacy) - Birebir veya Trimli
            if (string.Equals(stored, pass, StringComparison.Ordinal)) return true;
            if (string.Equals(stored, passTrimmed, StringComparison.Ordinal)) return true;
            if (string.Equals(stored, passTrimmed, StringComparison.OrdinalIgnoreCase)) return true;

            // 3. Ham SHA-256 (Tuzsuz / Base64 & Hex)
            using (var sha256 = SHA256.Create()) {
                var rawSha256B64 = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(passTrimmed)));
                if (string.Equals(stored, rawSha256B64, StringComparison.Ordinal)) return true;

                var rawSha256Hex = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(passTrimmed)));
                if (string.Equals(stored, rawSha256Hex, StringComparison.OrdinalIgnoreCase)) return true;

                var saltedSha256Hex = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(passTrimmed + Salt)));
                if (string.Equals(stored, saltedSha256Hex, StringComparison.OrdinalIgnoreCase)) return true;
            }

            // 4. MD5 (Base64 & Hex)
            using (var md5 = MD5.Create()) {
                var md5B64 = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(passTrimmed)));
                if (string.Equals(stored, md5B64, StringComparison.Ordinal)) return true;

                var md5Hex = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(passTrimmed)));
                if (string.Equals(stored, md5Hex, StringComparison.OrdinalIgnoreCase)) return true;
            }

            // 5. SHA-1 (Base64 & Hex)
            using (var sha1 = SHA1.Create()) {
                var sha1B64 = Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes(passTrimmed)));
                if (string.Equals(stored, sha1B64, StringComparison.Ordinal)) return true;

                var sha1Hex = Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(passTrimmed)));
                if (string.Equals(stored, sha1Hex, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        public static bool IsStrongPassword(string password, out string errorMessage) {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(password)) {
                errorMessage = "Şifre alanı boş bırakılamaz.";
                return false;
            }

            if (password.Length < 8) {
                errorMessage = "Şifreniz en az 8 karakter olmalıdır.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[A-Z]")) {
                errorMessage = "Şifreniz en az 1 adet büyük harf (A-Z) içermelidir.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[a-z]")) {
                errorMessage = "Şifreniz en az 1 adet küçük harf (a-z) içermelidir.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[0-9]")) {
                errorMessage = "Şifreniz en az 1 adet rakam (0-9) içermelidir.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]")) {
                errorMessage = "Şifreniz en az 1 adet özel sembol (!@#$%^&*) içermelidir.";
                return false;
            }

            return true;
        }
    }
}
