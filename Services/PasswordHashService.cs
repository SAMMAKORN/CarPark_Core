using System.Security.Cryptography;

namespace CarPark.Services
{
    public static class PasswordHashService
    {
        private const string Prefix = "PBKDF2$SHA256$";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Password is required.");
            }

            var salt = new byte[SaltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            var key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public static bool VerifyPassword(string storedPassword, string inputPassword)
        {
            if (string.IsNullOrWhiteSpace(storedPassword) || inputPassword is null)
            {
                return false;
            }

            if (!IsHashed(storedPassword))
            {
                return string.Equals(storedPassword, inputPassword, StringComparison.Ordinal);
            }

            var parts = storedPassword.Split('$');
            if (parts.Length != 5)
            {
                return false;
            }

            if (!int.TryParse(parts[2], out var iterations) || iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expectedKey;

            try
            {
                salt = Convert.FromBase64String(parts[3]);
                expectedKey = Convert.FromBase64String(parts[4]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualKey = Rfc2898DeriveBytes.Pbkdf2(
                inputPassword,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedKey.Length);

            return CryptographicOperations.FixedTimeEquals(expectedKey, actualKey);
        }

        public static bool IsHashed(string password)
        {
            return password.StartsWith(Prefix, StringComparison.Ordinal);
        }
    }
}
