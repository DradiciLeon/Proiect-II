using System.Linq;
using System.Text.RegularExpressions;

namespace Activity_Finder.Services
{
    public static class InputValidator
    {
        public static bool IsValidUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username)
                   && username.Length >= 3
                   && username.Length <= 30
                   && Regex.IsMatch(username, @"^[a-zA-Z0-9_.-]+$");
        }

        public static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email)
                   && email.Length <= 100
                   && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }

        public static bool IsStrongPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password)
                   && password.Length >= 8
                   && password.Length <= 100
                   && password.Any(char.IsUpper)
                   && password.Any(char.IsLower)
                   && password.Any(char.IsDigit);
        }
    }
}