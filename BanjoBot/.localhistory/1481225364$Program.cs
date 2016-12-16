using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Resources;
using System.Reflection;

namespace BanjoBot
{
    class Program
    {
        public static DataStore ds;
        private static Commands cmd = new Commands();
        private static DiscordClient bot;

        static void Main(string[] args)
        {
            // Create bot
            bot = new DiscordClient();

            // Set up the DataStore
            Console.WriteLine("Loading player stats...");
            ds = new DataStore();

            // Initialise commands
            Console.WriteLine("Initialising commands...");
            initialiseCommands();
            
            // Login to Discord
            bot.ExecuteAndWait(async () => {
                do {
                    Console.WriteLine("Please enter the bot's login credentials.");
                    Console.Write("Email: ");
                    String email = Console.ReadLine();
                    Console.Write("Password: ");
                    String password = Console.ReadLine();
                    Console.Clear();
                    Console.WriteLine("Logging in as " + email + "...");

                    try {
                        await bot.Connect("botpls@trash-mail.com", "qewradsf");
                    } catch (Discord.Net.HttpException e) {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Wrong credentials");
                    }
                } while (bot.State != ConnectionState.Connected);

                Console.Out.WriteLine("BanjoBot online.");
            });
        }

        /// <summary>
        /// Creates the commands.
        /// </summary>
        private static void initialiseCommands()
        {
            // Create command service
            var commandService = new CommandService(new CommandServiceConfigBuilder
            {
                PrefixChar = '!',
                HelpMode = HelpMode.Public
            });

            // Add command service
            var commands = bot.AddService(commandService);

            // Create !hostgame command
            commands.CreateCommand("hostgame")                                      
                    .Alias(new string[] { "host", "hg" })                                               // Command aliases, can be called with !host or !hg
                    .Description("Creates a new game (only one game may be in the lobby at a time).")   // Add description
                    .Do(async e =>                                                  
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.hostGame(e.Channel, user);
                    });

            // Create !join command
            commands.CreateCommand("join")
                    .Alias(new string[] { "j" })                                               
                    .Description("Joins the open game.")                              
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.joinGame(e.Channel, user);
                    });

            // Create !leave command
            commands.CreateCommand("leave")
                    .Alias(new string[] { "l" })
                    .Description("Leaves the open game.")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.leaveGame(e.Channel, user);
                    });

            // Create !cancel command
            commands.CreateCommand("cancelgame")
                    .Alias(new string[] { "cancel", "c" })
                    .Description("Cancels the open game.")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.cancelGame(e.Channel, user);
                    });

            // Create !votecancel command
            commands.CreateCommand("votecancel")
                    .Alias(new string[] { "vc" })
                    .Description("Casts a vote to cancel the open game.")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.voteCancel(e.Channel, user);
                    });

            // Create !startgame command
            commands.CreateCommand("startgame")
                    .Alias(new string[] { "start", "sg" })
                    .Description("Start the game. Host only, requires full game.")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.startGame(e.Channel, user);
                    });

            // Create !getplayers command
            commands.CreateCommand("getplayers")
                    .Alias(new string[] { "players", "list", "gp" })
                    .Description("Shows the players that have joined the open game.")
                    .Do(async e =>
                    {
                        await cmd.listPlayers(e.Channel);
                    });

            // Create !showstats command
            commands.CreateCommand("getstats")
                    .Alias(new string[] { "stats", "gs" })
                    .Description("Shows the league stats of a player.")
                    .Parameter("target", ParameterType.Required)                                            // As an argument. Target of the !getstats command
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.getStats(e.Channel, ds, e.GetArg("target"));
                    });

            // Create !getgames command
            commands.CreateCommand("getgames")
                    .Alias(new string[] { "games", "gg" })
                    .Description("Shows the status of all games.")
                    .Do(async e =>
                    {
                        await cmd.getGames(e.Channel);
                    });

            // Create !bluewins command
            commands.CreateCommand("bluewins")
                    .Alias(new string[] { "blue", "bw" })
                    .Description("Cast vote for Blue Team as the winner of game BBL#X (post game only).")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.voteWinner(e.Channel, user, Teams.Blue);
                    });

            // Create !redwins command
            commands.CreateCommand("redwins")
                    .Alias(new string[] { "red", "rw" })
                    .Description("Cast vote for Red Team as the winner of game BBL#X (post game only).")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.voteWinner(e.Channel, user, Teams.Red);
                    });

            // Create !draw command
            commands.CreateCommand("draw")
                    .Alias(new string[] { "d" })
                    .Description("Cast vote for a tied game for BBL#X (post game only).")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = ds.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = ds.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await cmd.voteWinner(e.Channel, user, Teams.Draw);
                    });

            // Create !topmmr command
            commands.CreateCommand("topmmr")
                    .Alias(new string[] { "top", "t" })
                    .Description("Shows the top 5 players by MMR.")
                    .Do(async e =>
                    {
                        await cmd.getTopMMR(e.Channel, ds);
                    });

            // Create !help command
            commands.CreateCommand("help")
                    .Alias(new string[] { "h" })
                    .Description("Shows the help.")
                    .Do(e =>
                    {
                        cmd.printHelp(e.User);
                    });

            // Create !save command
            commands.CreateCommand("save")
                    .Description("Saves the stats of all users. (Only useable by admins)")
                    .Do(e =>
                    {
                        cmd.saveData(e.User.Id);
                    });
        }
    }
}
