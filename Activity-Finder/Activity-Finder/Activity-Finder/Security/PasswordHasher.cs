using System;
using System.Security.Cryptography;

namespace Activity_Finder.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, Iterations, HashAlgorithmName.SHA256);

            byte[] key = pbkdf2.GetBytes(KeySize);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool VerifyPassword(string password, string storedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedPassword))
                return false;

            if (!IsHashedPassword(storedPassword))
                return password == storedPassword;

            try
            {
                string[] parts = storedPassword.Split('.');
                int iterations = int.Parse(parts[0]);
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] storedKey = Convert.FromBase64String(parts[2]);

                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password, salt, iterations, HashAlgorithmName.SHA256);

                byte[] key = pbkdf2.GetBytes(storedKey.Length);

                return CryptographicOperations.FixedTimeEquals(key, storedKey);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHashedPassword(string storedPassword)
        {
            string[] parts = storedPassword?.Split('.') ?? Array.Empty<string>();
            return parts.Length == 3 && int.TryParse(parts[0], out _);
        }
    }
}