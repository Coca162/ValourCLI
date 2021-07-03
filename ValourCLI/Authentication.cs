using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using static System.Console;
using static ValourCLI.Authentication;

namespace ValourCLI
{
    class Authentication
    {
        public static Config Config { get; set; }

        public enum ConfigSave
        {
            SaveAll,
            SaveEmail,
            SaveNone,
            NeverPassword,
            NeverAll
        }

        public enum Input
        {
            Yes,
            No,
            Never
        }

        public static void Login()
        {
            WriteLine("Detected no valid config file in this folder entering login");

            string email;
            while (true)
            {
                Write("Enter Email: ");
                email = ReadLine();

                if (IsValidEmail(email)) break;
                WriteLine("This email is not valid!");
            }

            Write("Enter Password: ");
            string password = ReadPassword();

            ConfigSave save = ConfigSave.SaveNone;

            Input AccountInput = InputDecision("\nDo you want this account to be remembered? yes/no/never: ");
            if (AccountInput == Input.Yes)
            {
                WriteLine("A file called config.json will be created in the directory this is being ran in");
                Input PasswordInput = InputDecision("Do you want this password to be remembered? yes/no/never: ");

                if (PasswordInput == Input.Yes) save = ConfigSave.SaveAll;
                else if (PasswordInput == Input.No) save = ConfigSave.SaveEmail;
                else if (PasswordInput == Input.Never) save = ConfigSave.NeverPassword;
            }
            else if (AccountInput == Input.Never)
            {
                WriteLine("A file called config.json will be created in the directory this is being ran in");
                save = ConfigSave.NeverAll;
            }

            switch (save)
            {
                case ConfigSave.SaveAll:
                    Config = new(email, password, true, true);
                    break;
                case ConfigSave.SaveEmail:
                    Config = new(email, "", true, true);
                    break;
                case ConfigSave.NeverPassword:
                    Config = new(email, "", true, false);
                    break;
                case ConfigSave.NeverAll:
                    Config = new("", "", false, false);
                    break;
            }

            if (save != ConfigSave.SaveNone) File.WriteAllText("config.json", JsonConvert.SerializeObject(Config));
        }

        public static Input InputDecision(string message)
        {
            while (true)
            {
                Write(message);
                string input = ReadLine().ToLower();

                switch (input)
                {
                    case "yes" or "y":
                        return Input.Yes;
                    case "no" or "n":
                        return Input.No;
                    case "never":
                        return Input.Never;
                    default:
                        WriteLine("Incorrect input");
                        continue;
                }
            }
        }

        public static string ReadPassword()
        {
            StringBuilder result = new();
            while (true)
            {
                ConsoleKeyInfo key = ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        if (result.Length != 0)
                        {
                            WriteLine();
                            return result.ToString();
                        }
                        WriteLine("Password cannot be empty!");
                        continue;
                    case ConsoleKey.Backspace:
                        if (result.Length == 0)
                        {
                            continue;
                        }
                        result.Length--;
                        Write("\b \b");
                        continue;
                    default:
                        result.Append(key.KeyChar);
                        Write("*");
                        continue;
                }
            }
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                return false;
            }
            catch (ArgumentException e)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
