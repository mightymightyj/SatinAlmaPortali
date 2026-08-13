using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace satinalmaportali2.Helpers {
    public static class PasswordHasher {
        private const string Salt = "SatinAlmaPortali_Secured_Salt_V1_2026";

        public static string HashPassword(string password) {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password + Salt);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string? storedHash) {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(password)) return false;
            
            string hashOfInput = HashPassword(password);
            // Check if matches hash or legacy plaintext password for backward compatibility
            return storedHash == hashOfInput || storedHash == password;
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
