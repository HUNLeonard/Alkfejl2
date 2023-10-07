using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Beadando.Models
{
    public class User
    {

        [Name("username")]
        public string? Username { get; set; } = string.Empty;
        [Name("password")]
        public string? PasswordHash { get; set; } = string.Empty;  // *hash* függvény
        [Name("email")]
        public string? Email { get; set; } = string.Empty;
        [Name("firstname")]
        public string? FirstName { get; set; } = string.Empty;
        [Name("lastname")]
        public string? LastName { get; set; } = string.Empty;

        public static string GenerateMainPasswdHash(string password)
        {

            // SHA-512 hash generálása
            byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hashedBytes = sha512.ComputeHash(passwordBytes);
                string base64UrlSafe = Convert.ToBase64String(hashedBytes);

                return base64UrlSafe;
            }
        }


    }


}
