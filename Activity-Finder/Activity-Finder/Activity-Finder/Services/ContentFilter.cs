using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Activity_Finder.Services
{
    public static class ContentFilter
    {
        private static readonly string[] BlockedWords =
        {
            "prost", "idiot", "fraier", "imbecil", "nesimtit", "dobitoc",
            "cretin", "ratat", "jeg", "gunoi", "penibil", "jalnic",
            "muie", "pula", "pul@", "plm", "pizda", "pzd", "fut", "futut",
            "fmm", "dracu", "dreq", "mortii", "mortii ma-tii",
            "ma-ta", "mata", "te fut", "du-te", "idiotule",

            "stupid", "dumb", "moron", "loser",
            "fuck", "fck", "fucking", "shit", "sh1t",
            "bitch", "b1tch", "asshole", "a$$hole",
            "bastard", "wtf", "nigger", "niga",

            "free money", "earn money fast", "click here",
            "buy now", "limited offer", "100% free",
            "win cash", "bonus now", "guaranteed money",
            "crypto profit", "investment scheme",

            "xxx", "porn", "sex", "onlyfans", "escort",
            "dating hot", "nude", "camgirl"
        };

        private static readonly string[] SuspiciousDomains =
        {
            "bit.ly", "tinyurl.com", "grabify", "free-money",
            "casino", "crypto", "onlyfans", "telegram", "t.me"
        };

        public static bool IsSafeText(string text, int maxLength, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(text))
            {
                errorMessage = "Textul nu poate fi gol.";
                return false;
            }

            if (text.Length > maxLength)
            {
                errorMessage = $"Textul este prea lung. Maxim {maxLength} caractere.";
                return false;
            }

            string lowered = text.ToLower();
            string normalized = NormalizeText(text);
            string noSpaces = NormalizeText(lowered.Replace(" ", ""));

            if (BlockedWords.Any(word =>
                lowered.Contains(word.ToLower()) ||
                normalized.Contains(NormalizeText(word)) ||
                noSpaces.Contains(NormalizeText(word))))
            {
                errorMessage = "Textul conține cuvinte nepotrivite.";
                return false;
            }

            if (Regex.IsMatch(lowered, @"(https?:\/\/|www\.)", RegexOptions.IgnoreCase))
            {
                errorMessage = "Linkurile nu sunt permise în postări.";
                return false;
            }

            if (SuspiciousDomains.Any(domain =>
                lowered.Contains(domain.ToLower()) ||
                normalized.Contains(NormalizeText(domain))))
            {
                errorMessage = "Textul conține linkuri sau domenii suspecte.";
                return false;
            }

            if (HasTooManyRepeatedCharacters(text))
            {
                errorMessage = "Textul pare spam.";
                return false;
            }

            // MODIFICAT: blochează valori gen "aaaa", "bbbb", "abcabcabc"
            if (HasLowCharacterVariety(text))
            {
                errorMessage = "Textul nu este valid.";
                return false;
            }

            if (text.Length > 10 && text.Count(char.IsUpper) > text.Length * 0.7)
            {
                errorMessage = "Nu folosi prea multe majuscule.";
                return false;
            }

            return true;
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = text.ToLower();

            text = text.Replace("@", "a")
                       .Replace("4", "a")
                       .Replace("1", "i")
                       .Replace("!", "i")
                       .Replace("3", "e")
                       .Replace("0", "o")
                       .Replace("$", "s")
                       .Replace("5", "s")
                       .Replace("7", "t");

            return new string(text
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private static bool HasTooManyRepeatedCharacters(string text)
        {
            int repeatCount = 1;

            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == text[i - 1])
                {
                    repeatCount++;

                    if (repeatCount >= 6)
                        return true;
                }
                else
                {
                    repeatCount = 1;
                }
            }

            return false;
        }

        private static bool HasLowCharacterVariety(string text)
        {
            string lettersOnly = new string(text
                .Where(char.IsLetter)
                .Select(char.ToLower)
                .ToArray());

            if (lettersOnly.Length < 4)
                return false;

            int distinctChars = lettersOnly.Distinct().Count();

            if (distinctChars <= 1)
                return true;

            if (distinctChars <= 2 && lettersOnly.Length >= 6)
                return true;

            string doubled = lettersOnly + lettersOnly;
            for (int length = 2; length <= lettersOnly.Length / 2; length++)
            {
                if (lettersOnly.Length % length == 0)
                {
                    string pattern = lettersOnly.Substring(0, length);
                    string repeated = string.Concat(Enumerable.Repeat(pattern, lettersOnly.Length / length));

                    if (repeated == lettersOnly)
                        return true;
                }
            }

            return false;
        }
    }
}