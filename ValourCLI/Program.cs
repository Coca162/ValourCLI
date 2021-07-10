using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System.IO;
using System.Threading.Tasks;
using static Valour.Net.ValourClient;
using static System.Console;
using static ValourCLI.Authentication;
using Valour.Net.Models;
using Valour.Net;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Client;
using Valour.Net.CommandHandling;

namespace ValourCLI
{
    class Program
    {
        public static ulong CurrentChannel { get; set; }
        public static int InputLocation = 0;
        public static Dictionary<PlanetMessage, int> messages = new();

        static async Task Main(string[] args)
        {
            WriteLine("Welcome to Valour CLI Version 0!");

            if (File.Exists("config.json"))
            {
                JSchemaGenerator generator = new();
                JSchema schema = generator.Generate(typeof(Config));

                string file = File.ReadAllText("config.json");

                JObject fileParse = JObject.Parse(file);

                if (fileParse.IsValid(schema)) Authentication.Config = JsonConvert.DeserializeObject<Config>(file);
                else Login();
            }
            else Login();

            await Start(Authentication.Config.Email, Authentication.Config.Password);

            Clear();

            WriteLine("Planets:");
            foreach (var planetCache in Cache.PlanetCache.Values)
            {
                WriteLine(planetCache.Name);
            }

            Write("Planet to enter: ");
            Planet planet;
            while (true)
            {
                string name = ReadLine();
                planet = Cache.PlanetCache.Values.Where(x => x.Name == name).FirstOrDefault();
                if (planet != null) break;
                WriteLine("You are not in this planet!");
            }

            WriteLine("Channels:");
            foreach (Channel channelCache in Cache.ChannelCache.Values.Where(x => x.Planet_Id == planet.Id))
            {
                WriteLine(channelCache.Name);
            }

            Write("Channel to enter: ");
            Channel channel;
            while (true)
            {
                string name = ReadLine();
                channel = Cache.ChannelCache.Values.Where(x => x.Name == name).FirstOrDefault();
                if (channel != null) break;
                WriteLine("This channel does not exist!");
            }

            Clear();

            await hubConnection.SendAsync("JoinPlanet", planet.Id, Token);
            await hubConnection.SendAsync("JoinChannel", channel.Id, Token);
            hubConnection.On<string>("Relay", OnRelay);

            while (true)
            {
                SetCursorPosition(0, WindowHeight + InputLocation);
                string msg = ReadLine().Replace(@"\n", "\n").Replace("\\\n", @"\n");
                SetCursorPosition(0, WindowHeight + InputLocation);
                Write(new string(' ', BufferWidth));
                SetCursorPosition(0, WindowHeight + InputLocation);
                await PostMessage(channel.Id, planet.Id, msg).ConfigureAwait(false);
            }

            await Task.Delay(-1);
        }

        public static async Task OnRelay(string data)
        {
            PlanetMessage message = JsonConvert.DeserializeObject<PlanetMessage>(data);
            messages.Add(message, message.Content.Split('\n').Length);
            int height = messages.Values.Sum();
            SetCursorPosition(0, height);
            message.Author = await message.GetAuthorAsync();
            message.Channel = await message.GetChannelAsync();
            message.Planet = await message.GetPlanetAsync();
            //CommandContext ctx = new();
            //await ctx.Set(message);
            WriteLine($"{message.Author.Nickname} {message.TimeSent.ToLocalTime().ToShortTimeString()}: {message.Content}");
            SetCursorPosition(CursorLeft, WindowHeight);

            if (WindowHeight < height)
            {
                InputLocation = height - WindowHeight;
            }
        }

        public static async Task Start(string email, string password)
        {
            await RequestTokenAsync(email, password);

            // get botid from token

            BotId = (await GetData<ValourUser>($"https://valour.gg/User/GetUserWithToken?token={Token}")).Id;

            await hubConnection.StartAsync();

            // load cache from Valour

            await Cache.UpdatePlanetAsync();

            List<Task> tasks = new();

            foreach (Planet planet in Cache.PlanetCache.Values)
            {
                tasks.Add(Task.Run(async () => await Cache.UpdateMembersFromPlanetAsync(planet.Id)));
                tasks.Add(Task.Run(async () => await Cache.UpdateChannelsFromPlanetAsync(planet.Id)));
                tasks.Add(Task.Run(async () => await Cache.UpdatePlanetRoles(planet.Id)));
            }

            await Task.WhenAll(tasks);

            // Sets every member's RoleNames for speed

            // use basic variable caching to improve the speed of this in the future

            foreach (PlanetMember member in Cache.PlanetMemberCache.Values)
            {
                foreach (ulong roleid in member.RoleIds)
                {
                    member.Roles.Add(Cache.PlanetCache.Values.First(x => x.Id == member.Planet_Id).Roles.First(x => x.Id == roleid));
                }
            }
        }
    }
}
