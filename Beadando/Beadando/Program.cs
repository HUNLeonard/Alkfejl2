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
        static string cfirstname = "";
        static string clastname = "";
        static string cwebuser = "";
        static string cwebpassword = "";
        static string cwebsite = "";

        static bool register = false;
        static bool list = false;
        static bool vault = false;
        static bool deleteuser = false;
        static bool deletevault = false;

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
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        EncryptedType e = new EncryptedType(sor.LogUser.Email, sor.PasswordEncrypted);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        Console.WriteLine($"Username: {sor.Username} | Password: {e.Decrypt().Secret} | Website: {sor.Website}");
                    }
                }
                else
                {
                    Console.WriteLine("\nHiba!\nA jelszó vagy a felhasználónév Autentikációnal HELYTELEN!");
                }

            }
            else if (vault)
            {
                if (UserExists(userCsvFilePath, cusername, cpassword))
                {
                    RegisterVaultUser(vaultCsvFilePath);
                }
                else
                {
                    Console.WriteLine("\nHiba!\nA jelszó vagy a felhasználónév Authentikációnal HELYTELEN!");
                }

            }
            else if (deleteuser)
            {
                if (UserExists(userCsvFilePath, cusername, cpassword))
                {
                    DeleteUser(userCsvFilePath, cusername);
                    DeleteUserVaultEntries(vaultCsvFilePath, cusername);
                    Console.WriteLine("\nA felhasználó és a hozzá tartozó jelszavak törlése sikeres volt.");
                }
                else
                {
                    Console.WriteLine("\nHiba!\nA jelszó vagy a felhasználónév Authentikációnal HELYTELEN VAGY felhasználó nem létezik!");
                }
            }
            else if (deletevault)
            {
                if (UserExists(userCsvFilePath, cusername, cpassword))
                {
                    DeleteUserVaultEntry(vaultCsvFilePath, cusername, cwebuser, cwebsite);
                }
                else
                {
                    Console.WriteLine("\nHiba!\nA jelszó vagy a felhasználónév autentikációval helytelen!");
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

        static void RegisterVaultUser(string vaultCsvFilePath)
        {
            try
            {
                // Check if 'vault.csv' file exists
                bool vaultCsvFileExists = File.Exists(vaultCsvFilePath);


                // Check if username already exists in the CSV file along with website
                if (vaultCsvFileExists)
                {
                    if(cwebsite.Length != 0)
                    {
                        using (var reader = new StreamReader(vaultCsvFilePath))
                        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                        {
                            var records = csv.GetRecords<VaultEntry>().ToList();
                            if (records.Any(u => u.Username == cwebuser) && records.Any(u => u.Website == cwebsite)) // && records.Any(u => u.Id == cusername)
                            {
                                Console.WriteLine($"\nAdott webre '{cwebsite}' már van valaki regisztrált ezzel a felhasználó névvel.");
                                return;
                            }
                        }
                    }
                }


                using (var writer = new StreamWriter(vaultCsvFilePath, true))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    
                    // If 'vault.csv' doesn't exist, write the header line
                    if (!vaultCsvFileExists)
                    {
                        csv.WriteField("user_id");
                        csv.WriteField("username");
                        csv.WriteField("password");
                        csv.WriteField("website");
                        csv.NextRecord();
                    }
                    
                    // Add a new user to 'user.csv'
                    VaultEntry vault = new VaultEntry();
                    vault.Id = cusername;
                    vault.Username = cwebuser;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    EncryptedType e = new EncryptedType(vault.LogUser.Email, cwebpassword);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    vault.PasswordEncrypted = e.Encrypt().Secret;
                    vault.Website = cwebsite;

                    
                    csv.WriteRecord(vault);
                    csv.NextRecord();

                    Console.WriteLine("\nA regisztráció a vault-ba sikeres volt.");   
                    Console.WriteLine($"WebUsername: {vault.Username}\nWebsite: {vault.Website}\n");

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("\nAn error occurred: " + ex.Message);
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
                            Console.WriteLine("\nHiba!\nA felhasználónév már foglalt :).");
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
                    user.FirstName = cfirstname;
                    user.LastName= clastname;


                    csv.WriteRecord(user);
                    csv.NextRecord();
                    Console.WriteLine("\nA regisztráció sikeres volt.");
                    Console.WriteLine($"\nUsername: {user.Username}\nEmail: {user.Email}\nFirstName: {user.FirstName}\nLastName: {user.LastName}");
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("\nAn error occurred: " + ex.Message);
            }
        }

        public static void DeleteUser(string UserCsvPath, string username)
        {
            if (UserCsvPath == null) return;

            var records = new List<User>();
            using (var reader = new StreamReader(UserCsvPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                records = csv.GetRecords<User>().ToList();
            }

            records.RemoveAll(user => user.Username == username);

            using (var writer = new StreamWriter(UserCsvPath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(records);
            }
        }

        public static void DeleteUserVaultEntries(string VaultCsvPath, string username)
        {
            if (VaultCsvPath == null) return;

            var records = new List<VaultEntry>();
            using (var reader = new StreamReader(VaultCsvPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                records = csv.GetRecords<VaultEntry>().ToList();
            }

            records.RemoveAll(entry => entry.Id == username);

            using (var writer = new StreamWriter(VaultCsvPath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(records);
            }
        }

        public static void DeleteUserVaultEntry(string VaultCsvPath, string userId, string webUsername, string website)
        {
            if (VaultCsvPath == null) return;

            var records = new List<VaultEntry>();
            using (var reader = new StreamReader(VaultCsvPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                records = csv.GetRecords<VaultEntry>().ToList();
            }

            // Töröljük az első olyan bejegyzést, amelynek azonosítója (Id) és webusername és website megegyezik a megadott értékekkel
            var entryToDelete = records.FirstOrDefault(entry => entry.Id == userId && entry.Username == webUsername && entry.Website == website);

            if (entryToDelete != null)
            {
                records.Remove(entryToDelete);

                using (var writer = new StreamWriter(VaultCsvPath))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    csv.WriteRecords(records);
                }
                Console.WriteLine("\nA webuser törlése az adott website-ról sikeres volt.");
            }
            else
            {
                Console.WriteLine($"\nNincs olyan bejegyzés, amely az adott webusername ({webUsername}) és website ({website}) kombinációt tartalmazza.");
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
                    Console.WriteLine("\nA felhasználónak nincsenek jelszavai a Vaultban elmentve.");
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
                Console.WriteLine("Használandó szintaxis: \ndotnet run 'kötelező' 'Kommandok' 'Opc. kommandok'\n\n" +
                    "Kötelező:\n" +
                    "--workdir=<arg>\n||Ezzel tudod megadni hogy hol kezelje a felhasználói adatokat.\n\n\n" +
                    "Kommandok:\n" +
                    "--register --username=<arg> --password=<arg> (+opciós kommand)\n||Regisztrál egy felhasználót.\n" +

                    "--list --username=<arg> --password=<arg>\n||Kilistázza a felhasználónak a különböző jelszavait weboldalakon.\n" +

                    "--vault --username=<arg> --password=<arg> --webuser=<arg> --webpassword=<arg> (+opciós kommand)\n" +
                    "||Weboldalakra való regisztrációs adatok elmentése.\n" +

                    "--deleteuser --username=<arg> --password=<arg>\n" +
                    "||User illetve Weboldalakra való regisztrációs adatok törlése.\n" +

                    "--deletevault --username=<arg> --password=<arg> --webuser=<arg> --website=<arg>\n" +
                    "||Weboldalra való regisztrált webuser törlése.\n\n\n" +

                    "Opcionális kommandok:\n" +
                    "Register:\n" +
                    "--email=<arg>\n|| Megadható felhasználó regisztációnál email is.\n" +
                    "--firstname=<arg>\n|| Megadható felhasználó regisztációnál firstname is.\n" +
                    "--lastname=<arg>\n|| Megadható felhasználó regisztációnál lastname is.\n\n" +
                    "Vault:\n" +
                    "--website=<arg>\n|| Megadható melyik webstite-ra szólnak a regisztációs adatok.\n\n");

                return;
            }

            if (!args[0].StartsWith("--workdir=") || args[0].Split("=")[1].Length == 0)
            {
                Console.WriteLine("\nHiba!\nNem adtál meg '--workdir=<arg>'-t !");
                return;
            }

            workdir = args[0].Split("=")[1];

            if (args.Length <= 1 || (!args[1].ToLower().Equals("--list") && !args[1].ToLower().Equals("--register") && !args[1].ToLower().Equals("--vault") && !args[1].ToLower().Equals("--deleteuser") && !args[1].ToLower().Equals("--deletevault")))
            {
                Console.WriteLine("\nHiba!\nNem '--list'/'--register'/'--vault'/'--deleteuser'/'--deletevault' -t írtál be '--workdir=<arg>' után!");
                return;
            }

            bool isRegister = args[1].ToLower().Contains("--register");
            bool isList = args[1].ToLower().Contains("--list");
            bool isVault = args[1].ToLower().Contains("--vault");
            bool isDeleteUser = args[1].ToLower().Contains("--deleteuser");
            bool isDeleteVault = args[1].ToLower().Contains("--deletevault");


            if (args.Length <= 2|| !args[2].StartsWith("--username=") || args[2].Split("=")[1].Length == 0)
            {
                Console.WriteLine("\nHiba!\nNem/Rosszul adtad meg '--username=<arg>'-t !\n");
                Console.WriteLine(GetRightCommand(isRegister,isList,isVault, isDeleteUser));
                return;
            }
            if (args.Length <= 3 || !args[3].StartsWith("--password=") || args[3].Split("=")[1].Length == 0)
            {
                Console.WriteLine("\nHiba!\nNem/Rosszul adtad meg '--password=<arg>'-t !");
                Console.WriteLine(GetRightCommand(isRegister, isList, isVault, isDeleteUser));
                return;
            }


            if (isRegister)
            {
                if (args.Length >= 5)
                {
                    if (!args[4].StartsWith("--email=") || args[4].Split("=")[1].Length == 0)
                    {
                        Console.WriteLine("\nHiba!\nRosszul adtad meg '--email=<arg>' -t !");
                        return;
                    }
                    cemail = args[4].Split("=")[1];
                }
                if (args.Length >= 6)
                {
                    if (!args[5].StartsWith("--firstname=") || args[5].Split("=")[1].Length == 0)
                    {
                        Console.WriteLine("\nHiba!\nRosszul adtad meg '--firstname=<arg>' -t !");
                        return;
                    }
                    cfirstname = args[5].Split("=")[1];
                }
                if (args.Length >= 7)
                {
                    if (!args[6].StartsWith("--lastname=") || args[6].Split("=")[1].Length == 0)
                    {
                        Console.WriteLine("\nHiba!\nRosszul adtad meg '--lastname=<arg>' -t !");
                        return;
                    }
                    clastname = args[6].Split("=")[1];
                }

                cusername = args[2].Split("=")[1];
                cpassword = args[3].Split("=")[1];
                register = true;
                return;
            }
            else if (isList)
            {
                cusername = args[2].Split("=")[1];
                cpassword = args[3].Split("=")[1];
                list = true;
                return;
            }
            else if (isDeleteUser)
            {
                cusername = args[2].Split("=")[1];
                cpassword = args[3].Split("=")[1];
                deleteuser = true;
                return;
            }
            else if (isDeleteVault)
            {
                
                if (args.Length <= 4)
                {
                    Console.WriteLine("\nHiba!\nNem adtál meg '--webuser=<arg> --website=<arg>' -t !");
                    return;
                }
                if (args.Length < 5 || !args[4].StartsWith("--webuser=") || args[4].Split("=")[1].Length == 0)
                {
                    Console.WriteLine($"\nHiba!\nNem/Rosszul adtad meg '--webuser=<arg>' -t !");
                    return;
                }
                if (args.Length < 6 || !args[5].StartsWith("--website="))
                {
                    Console.WriteLine("\nHiba!\nNem/Rosszul adtad meg '--website=<arg>' -t !");
                    return;
                }
                if (args[5].Split("=")[1].Length == 0)
                {
                    cwebsite = "";
                }
                else
                {
                    cwebsite = args[5].Split("=")[1];
                }

                cusername = args[2].Split("=")[1];
                cpassword = args[3].Split("=")[1];
                cwebuser = args[4].Split("=")[1];

                deletevault = true;
                return;
            }
            else if (isVault)
            {
                if (args.Length <= 4)
                {
                    Console.WriteLine("\nHiba!\nNem adtál meg '--webuser=<arg> --webpassword=<arg> (opcionális: '--website=<arg>')' -t !");
                    return;
                }
                if (args.Length < 5 || !args[4].StartsWith("--webuser=") || args[4].Split("=")[1].Length == 0)
                {
                    Console.WriteLine(args.Length >= 5);
                    Console.WriteLine($"\nHiba!\nNem adtál meg '--webuser=<arg>' -t !");
                    return;
                }
                if (args.Length < 6 || !args[5].StartsWith("--webpassword=") || args[5].Split("=")[1].Length == 0)
                {
                    Console.WriteLine("\nHiba!\nNem adtál meg '--webpassword=<arg> (opcionális: '--website=<arg>')' -t !");
                    return;
                }
                if (args.Length >= 7)
                {
                    if (!args[6].StartsWith("--website="))
                    {
                        Console.WriteLine("\nHiba!\nRosszul adtad meg '--website=<arg>' -t !");
                        return;
                    }
                    if(args[6].Split("=")[1].Length == 0) 
                    {
                        cwebsite = "";
                    }
                    else
                    {
                        cwebsite = args[6].Split("=")[1];
                    }
                }


                cusername = args[2].Split("=")[1];
                cpassword = args[3].Split("=")[1];
                cwebuser = args[4].Split("=")[1];
                cwebpassword = args[5].Split("=")[1];
                Console.WriteLine(cwebsite);
                vault = true;
                return;
            }

        }

        static string GetRightCommand(bool isRegister , bool isList, bool isVault, bool isDelete)
        {
            if (isRegister)
            {
                return ("Helyes szintaxis '--register' után: \n\n" +
                " --username=<arg> --password=<arg> (opcionális: '--email=<arg> --firstname=<arg> --lastname<arg>')");
            }
            else if (isList)
            {
                return ("Helyes szintaxis '--list' után: \n\n" +
                " --username=<arg> --password=<arg>");
            }
            else if (isVault)
            {
                return ("Helyes szintaxis '--vault' után: \n\n" +
                " --username=<arg> --password=<arg> --webuser=<arg> --webpassword=<arg> (opcionális: '--website=<arg>')");
            }
            else if (isDelete)
            {
                return ("Helyes szintaxis '--deleteuser' után: \n\n" +
                " --username=<arg> --password=<arg>");
            }
            return "";
        }


    }
}
