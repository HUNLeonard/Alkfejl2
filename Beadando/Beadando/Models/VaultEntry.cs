using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cryptography;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Net.Sockets;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace Beadando.Models
{
    public class VaultEntry
    {

        public static string? UserCsvPath { get; set; }

        [Name("user_id")]
        public string Id { get; set; } = string.Empty;

        [Ignore]
        public User? LogUser
        {
            get
            {
                if (UserCsvPath == null) return null;
                using StreamReader reader = new(UserCsvPath);
                using CsvReader csv = new(
                    reader, CultureInfo.InvariantCulture);
                return csv.GetRecords<User>()
                    .Where(el => el.Username == Id)
                    .FirstOrDefault();
            }
        }

        [Name("username")]
        public string Username { get; set; } = string.Empty;
        [Name("password")]
        public string PasswordEncrypted { get; set; } = string.Empty;// enkriptált forma
        [Name("website")]
        public string Website { get; set; } = string.Empty;




    }

    class EncryptedType
    {
        public string Key { get; set; } = string.Empty;     // user email
        public string Secret { get; set; } = string.Empty;  // password

        public EncryptedType(string email, string defpassword)
        {
            Key = email;
            Secret = defpassword;
        }

        public EncryptedType Encrypt()
        {
            using var hashing = SHA256.Create();
            byte[] keyHash = hashing.ComputeHash(Encoding.Unicode.GetBytes(Key));
            string key = Base64UrlEncoder.Encode(keyHash);
            string message = Base64UrlEncoder.Encode(Encoding.Unicode.GetBytes(Secret));
            return new(Key, Fernet.Encrypt(key, message));
        }

        public EncryptedType Decrypt()
        {
            using var hashing = SHA256.Create();
            byte[] keyHash = hashing.ComputeHash(Encoding.Unicode.GetBytes(Key));
            string key = Base64UrlEncoder.Encode(keyHash);
            string encodedSecret = Fernet.Decrypt(key, Secret);
            string message = Encoding.Unicode.GetString(Base64UrlEncoder.DecodeBytes(encodedSecret));
            return new(Key, message);
        }

    }
}
