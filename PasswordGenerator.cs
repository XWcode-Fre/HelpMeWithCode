using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DeltaBrowser
{
    public class PasswordGenerator
    {
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string NumberChars = "0123456789";
        private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        public static string GeneratePassword(
            int length = 16,
            bool includeLowercase = true,
            bool includeUppercase = true,
            bool includeNumbers = true,
            bool includeSpecial = true)
        {
            if (length < 8)
                throw new ArgumentException("Пароль должен быть не менее 8 символов");

            if (!includeLowercase && !includeUppercase && !includeNumbers && !includeSpecial)
                throw new ArgumentException("Должен быть выбран хотя бы один тип символов");

            var charSet = new StringBuilder();
            var password = new StringBuilder();

            if (includeLowercase) charSet.Append(LowercaseChars);
            if (includeUppercase) charSet.Append(UppercaseChars);
            if (includeNumbers) charSet.Append(NumberChars);
            if (includeSpecial) charSet.Append(SpecialChars);

            // Убеждаемся, что пароль содержит как минимум по одному символу каждого выбранного типа
            if (includeLowercase)
                password.Append(LowercaseChars[GetSecureRandomNumber(LowercaseChars.Length)]);
            if (includeUppercase)
                password.Append(UppercaseChars[GetSecureRandomNumber(UppercaseChars.Length)]);
            if (includeNumbers)
                password.Append(NumberChars[GetSecureRandomNumber(NumberChars.Length)]);
            if (includeSpecial)
                password.Append(SpecialChars[GetSecureRandomNumber(SpecialChars.Length)]);

            // Добавляем оставшиеся случайные символы
            string allPossibleChars = charSet.ToString();
            while (password.Length < length)
            {
                password.Append(allPossibleChars[GetSecureRandomNumber(allPossibleChars.Length)]);
            }

            // Перемешиваем символы в пароле
            return new string(password.ToString().ToCharArray().OrderBy(x => GetSecureRandomNumber(int.MaxValue)).ToArray());
        }

        private static int GetSecureRandomNumber(int maxValue)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomNumber = new byte[4];
                rng.GetBytes(randomNumber);
                return Math.Abs(BitConverter.ToInt32(randomNumber, 0)) % maxValue;
            }
        }
    }
} 