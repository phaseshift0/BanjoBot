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
        //TODO: remove _ls
        private const String BBL_SIGNUP_URL = "http://www.goo.gl/JbOfGE";
        private static DataStore _dataStore;
        private static LeagueController _ls;
        private static DiscordClient _bot;
        private static List<League> _leagues;
        private static String _allLeagues;
        private static List<Server> _connectedServers;
        private static List<MatchMakingServer> _allServers;

        static void Main(string[] args)
        {
            _bot = new DiscordClient();
            _bot.ServerAvailable += ServerConnected;
            _bot.ServerUnavailable += ServerDisconnected;
            _connectedServers = new List<Server>();
            _allServers = new List<MatchMakingServer>();

            // Initialise commands
            Console.WriteLine("Initialising commands...");
            InitialiseCommands(); 

            // Login to Discord
            _bot.ExecuteAndWait(async () =>
            {
                bool loginSuccessful = false;
                while (!loginSuccessful)
                {
                    Console.WriteLine("Login...");
                    try {
                        await _bot.Connect("MjU2NDg3NzU2MDkwNDQxNzI4.Cy-npA.zCjYPcnfTvqalIGcuewc951xXVY", TokenType.Bot);
                        loginSuccessful = true;
                    } catch (Discord.Net.HttpException e) {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Login failed");
                    }
                }
                Console.Out.WriteLine("BanjoBot online.");
            });
        }

        private static void LoadServerInformation(Server server)
        {
            Console.WriteLine("Downloading server information...");

            //TODO: load server information from sql server
            MatchMakingServer matchMakingServer = new MatchMakingServer(server);  //TODO: createServerFromSql, createLeagueFromSql, createPlayersfromSql
            if (matchMakingServer != null)
            {
                _allServers.Add(matchMakingServer);
                _ls = new LeagueController(matchMakingServer, null); //TODO: Move to MyServer Constructor _
            }
            else
            {
                
            }

            // Set up the DataStore
            Console.WriteLine("Loading player stats...");
            _dataStore = DataStore.GetInstance();
        }

        private static void ServerConnected(object sender, ServerEventArgs e)
        {
            if (!_connectedServers.Contains(e.Server))
            {
                _connectedServers.Add(e.Server);
                Console.WriteLine("Bot connected to a new server: " + e.Server.Name + "(" + e.Server.Id + ")");

                // Load server information if necessary 
                if(IsServerInitialised(e.Server))
                    LoadServerInformation(e.Server);

              
            }
        }

        private static bool IsServerInitialised(Server server)
        {
            foreach (MatchMakingServer myServer in _allServers)
            {
                if (myServer.DiscordServer.Id == server.Id)
                    return true;
            }

            return false;
        }

        private MatchMakingServer getMyServer(Server server)
        {
            foreach (MatchMakingServer myServer in _allServers) {
                if (myServer.DiscordServer.Id == server.Id)
                    return myServer;
            }

            return null;
        }

        private static void ServerDisconnected(object sender, ServerEventArgs e)
        {
            _connectedServers.Remove(e.Server);
        }


        private static async void CommandError(object sender, CommandErrorEventArgs e)
        {
            if(e.Exception.Message != "")
                await e.User.SendMessage(e.Exception.Message);
        }

     

        /// <summary>
        /// Creates the commands.
        /// </summary>
        private static void InitialiseCommands()
        {
            // TODO: AddCheck to all commands (if Server is not in _AllServers)
            // TODO: CheckIFRegistered should check if users is registered for the _correct_ server
            // TODO: CheckIFLeagueChannel check league channel for the _correct_ server
            // Create command service
            var commandService = new CommandService(new CommandServiceConfigBuilder
            {
                PrefixChar = '!',
                HelpMode = HelpMode.Public
            });

            // Add command service
            commandService.CommandErrored += CommandError;
            var commands = _bot.AddService(commandService);

            // Create !hostgame command
            commands.CreateCommand("hostgame")                                      
                    .Alias(new string[] { "host", "hg" })                               
                    .Description("Creates a new game (only one game may be in the lobby at a time).")
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel, "This is no league channel. Join " + _allLeagues + " and try again")
                    .Do(async e =>                                                  
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null) 
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.hostGame(e.Channel, user);
                    });

            // Create !join command
            commands.CreateCommand("join")
                    .Alias(new string[] { "j" })                                               
                    .Description("Joins the open game.")
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel, "This is no league channel. Join " + _allLeagues + " and try again")
                    .Do(async e =>
                    {
                        // Get or create user
                        Console.WriteLine("Tries to join");
                        Player user;
                        if (e.User.Nickname != null)
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.joinGame(e.Channel, user);
                    });

            // Create !leave command
            commands.CreateCommand("leave")
                    .Alias(new string[] { "l" })
                    .Description("Leaves the open game.")
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.leaveGame(e.Channel, user);
                    });

            // Create !cancel command
            commands.CreateCommand("cancelgame")
                    .Alias(new string[] { "cancel", "c" })
                    .Description("Cancels the open game.")
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.cancelGame(e.Channel, user);
                    });

            // Create !votecancel command
            commands.CreateCommand("votecancel")
                    .Alias(new string[] { "vc" })
                    .Description("Casts a vote to cancel the open game.")
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.voteCancel(e.Channel, user);
                    });

            // Create !startgame command
            commands.CreateCommand("startgame")
                    .Alias(new string[] { "start", "sg" })
                    .Description("Start the game. Host only, requires full game.")
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.startGame(e.Channel, user);
                    });

            // Create !getplayers command
            commands.CreateCommand("getplayers")
                    .Alias(new string[] { "players", "list", "gp" })
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Description("Shows the players that have joined the open game.")
                    .Do(async e =>
                    {
                        await _ls.listPlayers(e.Channel);
                    });

            // Create !showstats command
            commands.CreateCommand("getstats")
                    .Alias(new string[] { "stats", "gs" })
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Description("Shows the league stats of a player.")
                    .Parameter("target", ParameterType.Required)  
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.getStats(e.Channel, _dataStore, e.GetArg("target"));
                    });

            // Create !getgames command
            commands.CreateCommand("getgames")
                    .Alias(new string[] { "games", "gg" })
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Description("Shows the status of all games.")
                    .Do(async e =>
                    {
                        await _ls.getGames(e.Channel);
                    });

            // Create !bluewins command
            commands.CreateCommand("bluewins")
                    .Alias(new string[] { "blue", "bw" })
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Description("Cast vote for Blue Team as the winner of game BBL#X (post game only).")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.voteWinner(e.Channel, user, Teams.Blue);
                    });

            // Create !redwins command
            commands.CreateCommand("redwins")
                    .Alias(new string[] { "red", "rw" })
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Description("Cast vote for Red Team as the winner of game BBL#X (post game only).")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.voteWinner(e.Channel, user, Teams.Red);
                    });

            // Create !draw command
            commands.CreateCommand("draw")
                    .Alias(new string[] { "d" })
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Description("Cast vote for a tied game for BBL#X (post game only).")
                    .Do(async e =>
                    {
                        // Get or create user
                        Player user;
                        if (e.User.Nickname != null)
                            user = _dataStore.getPlayer(e.User.Id, e.User.Nickname, e.User.NicknameMention, e.User);
                        else
                            user = _dataStore.getPlayer(e.User.Id, e.User.Name, e.User.Mention, e.User);

                        await _ls.voteWinner(e.Channel, user, Teams.Draw);
                    });

            // Create !topmmr command
            commands.CreateCommand("topmmr")
                    .Alias(new string[] { "top", "t" })
                    .AddCheck(CheckIfRegistered, "You are not signed up. Sign up here: " + BBL_SIGNUP_URL + " and contact a League Moderator")
                    .AddCheck(CheckIfLeagueChannel)
                    .Description("Shows the top 5 players by MMR.")
                    .Do(async e =>
                    {
                        await _ls.getTopMMR(e.Channel, _dataStore);
                    });

            // TODO: Create !help command
            commands.CreateCommand("help")
                    .Alias(new string[] { "h" })
                    .Description("Shows the help.")
                    .Do(e =>
                    {
                        //_ls.printHelp(e.User);
                    });

            // TODO:  Create !getDetailedStats
            commands.CreateCommand("getDetailedStats")
                   .Alias(new string[] { "h" })
                   .Description("Shows the help.")
                   .Do(e => {
                      
                   });



            // Admin commands
            // TODO: Create !createLeague command
            // No parameter -> current Channel, Parameter: #channel
            commands.CreateCommand("createLeague")
                    .Description("Saves the stats of all users. (Only useable by admins)")
                    .Do(e => {
                
                    });

            // TODO: Create !registerLeague command
            commands.CreateCommand("registerLeague")
                    .Description("Saves the stats of all users. (Only useable by admins)")
                    .Do(e => {
                        
                    });

            // TODO: Create !createModChannel
            // Registers all roles with write permission to mod roles
            commands.CreateCommand("createModChannel")
                    .Description("Saves the stats of all users. (Only useable by admins)")
                    .Do(e => {
                      
                    });

            // TODO: Create !registerModChannel
            // Registers all roles with write permission to mod roles
            commands.CreateCommand("registerModChannel")
                    .Description("Saves the stats of all users. (Only useable by admins)")
                    .Do(e => {
                      
                    });

            // TODO: Create !warn
            commands.CreateCommand("warn")
                    .Description("Saves the stats of all users. (Only useable by admins)")
                    .Do(e => {
                        
                    });

            // TODO: Create !setAutoSeasons
            // Automated starting and ending of all Seasons
            commands.CreateCommand("setAutoLeague")
                    .Description("Saves the stats of all users. (Only useable by admins)")
                    .Do(e => {
                      
                    });

            // TODO: Create !startSeason
            commands.CreateCommand("startSeason")
                    .Description("Saves the stats of all users. (Only useable by admins)")
                    .Do(e => {
                       
                    });

            // TODO: Create !stopSeason
            commands.CreateCommand("stopSeason")
                    .Description("Saves the stats of all users. (Only useable by admins)")
                    .Do(e => {
                        _ls.saveData(e.User.Id);
                    });
        }

        private static bool CheckIfRegistered(Command command, User user, Channel channel)
        {
            foreach(League league in _leagues)
            {
                if (user.HasRole(league.Role))
                {
                    return true;
                }
                   
            }
            return false;
        }

        private static bool CheckIfLeagueChannel(Command command, User user, Channel channel) {
            foreach (League league in _leagues) {
                if (league.Channel == channel)
                    return true;
            }
            return false;
        }
    }
}
