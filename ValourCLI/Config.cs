using Newtonsoft.Json;

namespace ValourCLI
{
    public class Config
    {
        [JsonProperty("email", Required = Required.Always)]
        public string Email { get; }
        [JsonProperty("password", Required = Required.Always)]
        public string Password { get; }
        [JsonProperty("saveAccount", Required = Required.Always)]
        public bool SaveAccount { get; }
        [JsonProperty("savePassword", Required = Required.Always)]
        public bool SavePassword { get; }

        public Config(string email, string password, bool saveAccount, bool savePassword)
        {
            Email = email;
            Password = password;
            SaveAccount = saveAccount;
            SavePassword = savePassword;
        }
    }
}