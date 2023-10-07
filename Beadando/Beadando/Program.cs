using System;
using System.Globalization;
using System.Linq;
using Beadando.Models;
using CsvHelper;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.IdentityModel.Tokens;
using CsvHelper.Configuration;

namespace Beadando
{
    class Program
    {
        static string workdir = "";
        static string cusername = "";
        static string cpassword = "";
        static string cemail = "";


        static bool register = false;
        static bool list = false;

        static void Main(string[] args)
        {
            ArgsManager(args);

            string dbDirectory = Path.Combine(workdir, "db");
            string userCsvFilePath = Path.Combine(workdir, "user.csv");
            string vaultCsvFilePath = Path.Combine(workdir, "vault.csv");

            if (!File.Exists(userCsvFilePath) && !File.Exists(vaultCsvFilePath))
            {
                if (!Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                    userCsvFilePath = Path.Combine(dbDirectory, "user.csv");
                    vaultCsvFilePath = Path.Combine(dbDirectory, "vault.csv");
                }
                else
                {
                    userCsvFilePath = Path.Combine(dbDirectory, "user.csv");
                    vaultCsvFilePath = Path.Combine(dbDirectory, "vault.csv");
                }
            }

            VaultEntry.UserCsvPath = userCsvFilePath;

            if (register)
            {
                RegisterUser(userCsvFilePath);
            }
            else if (list)
            {
                if (UserExists(userCsvFilePath, cusername, cpassword)){
                    foreach (var sor in GetVaultEntriesForUser(cusername, vaultCsvFilePath)) 
                    {
                        EncryptedType e = new EncryptedType(sor.LogUser.Email, sor.PasswordEncrypted);

                        Console.WriteLine($"Username: {sor.Username} | Password: {e.Decrypt().Secret} | Website: {sor.Website}");
                    }
                }
                else
                {
                    Console.WriteLine("A jelszó vagy a felhasználónév HELYTELEN!");
                }

            }
        }


        public static bool UserExists(string UserCsvPath, string username, string password)
        {
            if (UserCsvPath == null) return false;

            using (var reader = new StreamReader(UserCsvPath))
            using (CsvReader csv = new(reader, CultureInfo.InvariantCulture))
            {
                var users = csv.GetRecords<User>().ToList();
                string hashedPassword = User.GenerateMainPasswdHash(password);
                return users.Any(user => user.Username == username && user.PasswordHash == hashedPassword);
            }
        }


        static void RegisterUser(string userCsvFilePath)
        {
            try
            {
                // Check if 'user.csv' file exists
                bool userCsvFileExists = File.Exists(userCsvFilePath);


                // Check if username already exists in the CSV file
                if (userCsvFileExists)
                {
                    using (var reader = new StreamReader(userCsvFilePath))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                    {
                        var records = csv.GetRecords<User>().ToList();
                        if (records.Any(u => u.Username == cusername))
                        {
                            Console.WriteLine("A felhasználónév már foglalt :).");
                            return;
                        }
                    }
                }

                using (var writer = new StreamWriter(userCsvFilePath, true))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    // If 'user.csv' doesn't exist, write the header line
                    if (!userCsvFileExists)
                    {
                        csv.WriteField("username");
                        csv.WriteField("password");
                        csv.WriteField("email");
                        csv.WriteField("firstname");
                        csv.WriteField("lastname");
                        csv.NextRecord();
                    }

                    // Add a new user to 'user.csv'
                    User user = new User();
                    user.Username = cusername;
                    user.PasswordHash = User.GenerateMainPasswdHash(cpassword);
                    user.Email = cemail;


                    csv.WriteRecord(user);
                    csv.NextRecord();
                    Console.WriteLine("A regisztráció sikeres volt.");
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }


        public static List<VaultEntry> GetVaultEntriesForUser(string username, string VaultCsvPath)
        {
            if (VaultCsvPath == null) return new List<VaultEntry>();

            using (StreamReader reader = new(VaultCsvPath))
            using (CsvReader csv = new(reader, CultureInfo.InvariantCulture))
            {
                var users = csv.GetRecords<VaultEntry>().ToList();

                if (!users.Any(user => user.Id == username))
                {
                    Console.WriteLine("A felhasználónak, nincsenek jelszavai a Vaultban elmentve.");
                    return new List<VaultEntry>();
                }
            }

            using (StreamReader reader = new StreamReader(VaultCsvPath))
            using (CsvReader csv = new(reader, CultureInfo.InvariantCulture))
            {
                var vaultEntries = csv.GetRecords<VaultEntry>().ToList();

                return vaultEntries
                    .Where(entry => entry.Id == username)
                    .ToList();
            }
        }


        static void ArgsManager(string[] args)
        {

            if (args.Length == 0)
            {
                Console.WriteLine("Használható szintaxis: \n'dotnet run 'COMMANDS' !\n" +
                    "Kötelező:\n" +
                    "--workdir=<arg>    |   Ezzel tudod megadni hogy hol kezelje a felhasználói adatokat.\n\n" +
                    "Kommandok:\n" +
                    "--register --username=<arg> --password=<arg>   |   Regisztáció.\n" +
                    "--list --username=<arg> --password=<arg>       |   Kilistázza a VaultEntry-ből a regisztált accountokat.\n\n" +
                    "Opcionális:\n" +
                    "--email=<arg>      |   A register kommand végére még emailt is meglehet adni!");
                return;
            }

            if (!args[0].StartsWith("--workdir=") || args[0].Split("=")[1].Length == 0)
            {
                Console.WriteLine("Nem adtál meg '--workdir=<arg>'-t !");
                return;
            }

            workdir = args[0].Split("=")[1];

            if (args.Length < 2 || (!args[1].Contains("--list") && !args[1].Contains("--register")))
            {
                Console.WriteLine("Hiba! Nem '--list' vagy '--register' -t írtál be!");
                return;
            }

            bool isRegister = args[1].Contains("--register");
            bool isList = args[1].Contains("--list");

            if (!isRegister && !isList)
            {
                Console.WriteLine("Hiba! Nem '--list' vagy '--register' -t írtál be!");
                return;
            }

            if ((isList || isRegister) && args.Length < 4)
            {
                Console.WriteLine("Nem adtál meg '--username=<arg> --password=<arg>'!");
                return;
            }

            if (args[2].StartsWith("--username=") && args[2].Split("=")[1].Length > 0)
            {
                if (args[3].StartsWith("--password=") && args[3].Split("=")[1].Length > 0)
                {
                    if (isRegister)
                    {

                        cusername = args[2].Split("=")[1];
                        cpassword = args[3].Split("=")[1];
                        register = true;
                        if (args.Length > 4)
                        {
                            if (args[4].StartsWith("--email=") && args[4].Split("=")[1].Length > 0)
                            {
                                cemail = args[4].Split("=")[1];
                            }
                        }



                    }
                    else if (isList)
                    {
                        cusername = args[2].Split("=")[1];
                        cpassword = args[3].Split("=")[1];
                        list = true;
                    }
                }
                else
                {
                    Console.WriteLine("Nem adtál meg '--password=<arg>'-t !");
                    return;
                }
            }
            else
            {
                Console.WriteLine("Nem adtál meg '--username=<arg>'-t !");
                return;
            }

        }


    }
}
