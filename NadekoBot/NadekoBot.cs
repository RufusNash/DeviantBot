using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes;
using NadekoBot.Classes.Help.Commands;
using NadekoBot.Classes.JSONModels;
using NadekoBot.Modules.Administration;
using NadekoBot.Modules.Conversations;
using NadekoBot.Modules.Help;
using NadekoBot.Modules.Permissions;
using NadekoBot.Modules.Permissions.Classes;
using NadekoBot.Modules.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot
{
    public class NadekoBot
    {
        public static DiscordClient Client { get; private set; }
        public static Credentials Creds { get; set; }
        public static Configuration Config { get; set; }
        public static LocalizedStrings Locale { get; set; } = new LocalizedStrings();
        public static string BotMention { get; set; } = "";
        public static bool Ready { get; set; } = false;
        public static Action OnReady { get; set; } = delegate { };

        private static List<Channel> OwnerPrivateChannels { get; set; }

        private static void Main()
        {
            Console.OutputEncoding = Encoding.Unicode;

            try
            {
                File.WriteAllText("data/config_example.json", JsonConvert.SerializeObject(new Configuration(), Formatting.Indented));
                if (!File.Exists("data/config.json"))
                    File.Copy("data/config_example.json", "data/config.json");
                File.WriteAllText("credentials_example.json", JsonConvert.SerializeObject(new Credentials(), Formatting.Indented));

            }
            catch
            {
                Console.WriteLine("Failed writing credentials_example.json or data/config_example.json");
            }

            try
            {
                Config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("data/config.json"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed loading configuration.");
                Console.WriteLine(ex);
                Console.ReadKey();
                return;
            }

            try
            {
                //load credentials from credentials.json
                Creds = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText("credentials.json"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load stuff from credentials.json, RTFM\n{ex.Message}");
                Console.ReadKey();
                return;
            }

            //if password is not entered, prompt for password
            if (string.IsNullOrWhiteSpace(Creds.Token))
            {
                Console.WriteLine("Token blank. Please enter your bot's token:\n");
                Creds.Token = Console.ReadLine();
            }
            Console.WriteLine(Config.ForwardMessages != true
                ? "Not forwarding messages."
                : "Forwarding private messages to owner.");

            BotMention = $"<@{Creds.BotId}>";

            //create new discord client and log
            Client = new DiscordClient(new DiscordConfigBuilder()
            {
                MessageCacheSize = 10,
                ConnectionTimeout = int.MaxValue,
                LogLevel = LogSeverity.Warning,
                LogHandler = (s, e) =>
                    Console.WriteLine($"Severity: {e.Severity}" +
                                      $"ExceptionMessage: {e.Exception?.Message ?? "-"}" +
                                      $"Message: {e.Message}"),
            });

            //create a command service
            var commandService = new CommandService(new CommandServiceConfigBuilder
            {
                AllowMentionPrefix = false,
                CustomPrefixHandler = m => 0,
                HelpMode = HelpMode.Disabled,
                ErrorHandler = async (s, e) =>
                {
                    if (e.ErrorType != CommandErrorType.BadPermissions)
                        return;
                    if (string.IsNullOrWhiteSpace(e.Exception?.Message))
                        return;
                    try
                    {
                        await e.Channel.SendMessage(e.Exception.Message).ConfigureAwait(false);
                    }
                    catch { }
                }
            });

            //add command service
            Client.AddService<CommandService>(commandService);

            //create module service
            var modules = Client.AddService<ModuleService>(new ModuleService());

            //add audio service
            Client.AddService<AudioService>(new AudioService(new AudioServiceConfigBuilder()
            {
                Channels = 2,
                EnableEncryption = false,
                Bitrate = 128,
            }));

            //install modules
            modules.Add(new HelpModule(), "Help", ModuleFilter.None);
            modules.Add(new AdministrationModule(), "Administration", ModuleFilter.None);
            modules.Add(new UtilityModule(), "Utility", ModuleFilter.None);
            modules.Add(new PermissionModule(), "Permissions", ModuleFilter.None);
            modules.Add(new Conversations(), "Conversations", ModuleFilter.None);

            
            //run the bot
            Client.ExecuteAndWait(async () =>
            {
                await Task.Run(() =>
                {
                    Console.WriteLine("Specific config started initializing.");
                    var x = SpecificConfigurations.Default;
                    Console.WriteLine("Specific config done initializing.");
                });

                await PermissionsHandler.Initialize();

                try
                {
                    await Client.Connect(Creds.Token, TokenType.Bot).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Token is wrong. Don't set a token if you don't have an official BOT account.");
                    Console.WriteLine(ex);
                    Console.ReadKey();
                    return;
                }
#if NADEKO_RELEASE
                await Task.Delay(300000).ConfigureAwait(false);
#else
                await Task.Delay(1000).ConfigureAwait(false);
#endif

                Console.WriteLine("-----------------");
                Console.WriteLine(await NadekoStats.Instance.GetStats().ConfigureAwait(false));
                Console.WriteLine("-----------------");


                OwnerPrivateChannels = new List<Channel>(Creds.OwnerIds.Length);
                foreach (var id in Creds.OwnerIds)
                {
                    try
                    {
                        OwnerPrivateChannels.Add(await Client.CreatePrivateChannel(id).ConfigureAwait(false));
                    }
                    catch
                    {
                        Console.WriteLine($"Failed creating private channel with the owner {id} listed in credentials.json");
                    }
                }
                Client.ClientAPI.SendingRequest += (s, e) =>
                {
                    var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
                    if (request == null) return;
                    // meew0 is magic
                    request.Content = request.Content?.Replace("@everyone", "@everyοne").Replace("@here", "@һere") ?? "_error_";
                    if (string.IsNullOrWhiteSpace(request.Content))
                        e.Cancel = true;
                };
#if NADEKO_RELEASE
                Client.ClientAPI.SentRequest += (s, e) =>
                {
                    Console.WriteLine($"[Request of type {e.Request.GetType()} sent in {e.Milliseconds}]");

                    var request = e.Request as Discord.API.Client.Rest.SendMessageRequest;
                    if (request == null) return;

                    Console.WriteLine($"[Content: { request.Content }");
                };
#endif
                NadekoBot.Ready = true;
                NadekoBot.OnReady();
                Console.WriteLine("Ready!");
                //reply to personal messages and forward if enabled.
                Client.MessageReceived += Client_MessageReceived;
            });
            Console.WriteLine("Exiting...");
            Console.ReadKey();
        }

        public static bool IsOwner(ulong id) => Creds.OwnerIds.Contains(id);

        public static async Task SendMessageToOwner(string message)
        {
            if (Config.ForwardMessages && OwnerPrivateChannels.Any())
                if (Config.ForwardToAllOwners)
                    OwnerPrivateChannels.ForEach(async c =>
                    {
                        try { await c.SendMessage(message).ConfigureAwait(false); } catch { }
                    });
                else
                {
                    var c = OwnerPrivateChannels.FirstOrDefault();
                    if (c != null)
                        await c.SendMessage(message).ConfigureAwait(false);
                }
        }

        private static bool repliedRecently = false;
        private static async void Client_MessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Server != null || e.User.Id == Client.CurrentUser.Id) return;
                if (ConfigHandler.IsBlackListed(e))
                    return;

                if (Config.ForwardMessages && !NadekoBot.Creds.OwnerIds.Contains(e.User.Id) && OwnerPrivateChannels.Any())
                    await SendMessageToOwner(e.User + ": ```\n" + e.Message.Text + "\n```").ConfigureAwait(false);

                if (repliedRecently) return;

                repliedRecently = true;
                if (e.Message.RawText != NadekoBot.Config.CommandPrefixes.Help + "h")
                    await e.Channel.SendMessage(HelpCommand.DMHelpString).ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false);
                repliedRecently = false;
            }
            catch { }
        }
    }
}
