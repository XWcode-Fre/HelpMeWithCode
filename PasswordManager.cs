using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace DeltaBrowser
{
    public class PasswordManager
    {
        private readonly SQLiteConnection _db;
        private readonly byte[] _key = Encoding.UTF8.GetBytes("DeltaBrowserSecureKey123!@#$%^&*()"); // В реальном приложении используйте более безопасный способ хранения ключа

        public PasswordManager(SQLiteConnection db)
        {
            _db = db;
        }

        public void SavePassword(string domain, string username, string password)
        {
            string encryptedPassword = EncryptPassword(password);
            string sql = @"INSERT OR REPLACE INTO Passwords (Domain, Username, Password) 
                          VALUES (@domain, @username, @password)";

            using (SQLiteCommand command = new SQLiteCommand(sql, _db))
            {
                command.Parameters.AddWithValue("@domain", domain);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", encryptedPassword);
                command.ExecuteNonQuery();
            }
        }

        public (string username, string password) GetPassword(string domain)
        {
            string sql = "SELECT Username, Password FROM Passwords WHERE Domain = @domain";
            using (SQLiteCommand command = new SQLiteCommand(sql, _db))
            {
                command.Parameters.AddWithValue("@domain", domain);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string username = reader.GetString(0);
                        string encryptedPassword = reader.GetString(1);
                        return (username, DecryptPassword(encryptedPassword));
                    }
                }
            }
            return (null, null);
        }

        private string EncryptPassword(string password)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.GenerateIV();

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
                    byte[] resultBytes = new byte[aes.IV.Length + encryptedBytes.Length];
                    
                    Buffer.BlockCopy(aes.IV, 0, resultBytes, 0, aes.IV.Length);
                    Buffer.BlockCopy(encryptedBytes, 0, resultBytes, aes.IV.Length, encryptedBytes.Length);
                    
                    return Convert.ToBase64String(resultBytes);
                }
            }
        }

        private string DecryptPassword(string encryptedPassword)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
            
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                byte[] iv = new byte[aes.IV.Length];
                byte[] cipherText = new byte[encryptedBytes.Length - aes.IV.Length];
                
                Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(encryptedBytes, iv.Length, cipherText, 0, cipherText.Length);
                
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
    }
} 