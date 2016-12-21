using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Extensions.MonoHttp;

namespace BanjoBot
{
    public class Program
    {
        //TODO: multi channel multi role league 
        private const String TOKEN = "MjU2NDg3NzU2MDkwNDQxNzI4.CzcTaA.VwuRyn--VPREZUtdr300f5FaQwo";
        private const String BBL_SIGNUP_URL = "http://www.goo.gl/JbOfGE";
        private const String BASE_SERVER_URL = "http://127.0.0.1/";
        private DiscordSocketClient _bot;
        private List<SocketGuild> _connectedServers;
        private List<MatchMakingServer> _allServers;
        private DatabaseController _databaseController;
        private CommandService _commands;
        private DependencyMap _commandMap;

        public static void Main(string[] args) => new Program().Run().GetAwaiter().GetResult();

        public async Task Run() 
        {
            _bot = new DiscordSocketClient();
            _bot.GuildAvailable += ServerConnected;
            _bot.GuildUnavailable += ServerDisconnected;
            _bot.MessageReceived += BotOnMessageReceived;
            _connectedServers = new List<SocketGuild>();
            _allServers = new List<MatchMakingServer>();
            _databaseController = new DatabaseController(new DependencyMap());

            // Initialise commands
            Console.WriteLine("Initialising commands...");
            CommandServiceConfig commandConfig = new CommandServiceConfig();
            commandConfig.CaseSensitiveCommands = false;
            _commands = new CommandService();
            _commandMap = new DependencyMap();
            await InitialiseCommands(); 

            await _bot.LoginAsync(TokenType.Bot, TOKEN);
            await _bot.ConnectAsync();
            await Task.Delay(-1);
        }

        private async Task LoadServerInformation(SocketGuild server)
        {
            Console.WriteLine("Downloading server information...");        
            MatchMakingServer matchMakingServer = await _databaseController.GetServer(server);
            if (matchMakingServer != null)
            {
                _allServers.Add(matchMakingServer);
                LoadPlayerBase(matchMakingServer);
            }
            else
            {
                matchMakingServer = createNewServer(server);
                _allServers.Add(matchMakingServer);
            }
            Console.WriteLine("Server is ready!");
        }

        private void LoadPlayerBase(MatchMakingServer server)
        {
            Console.WriteLine("Load Playerbase...");
            List<Player> allPlayers = _databaseController.GetPlayerBase(server);
            foreach (var player in allPlayers)
            {
                server.RegisteredPlayers.Add(player);
                foreach (var leagueController in server.LeagueController)
                {
                    foreach (var playerLeagueStat in player.LeagueStats)
                    {
                        if (playerLeagueStat.LeagueID == leagueController.League.LeagueID)
                        {
                            leagueController.League.RegisteredPlayers.Add(player);
                        }
                    }
                }
            }
        }

        private MatchMakingServer createNewServer(SocketGuild server)
        {
            int leagueID = _databaseController.InsertNewLeague(server.Id);
            return new MatchMakingServer(server, new League(leagueID, 1, null, null));
        }

        private static void UserRegistration()
        {
            
        }

        private async Task ServerConnected(SocketGuild server)
        {
            if (!_connectedServers.Contains(server))
            {
                _connectedServers.Add(server);
                Console.WriteLine("Bot connected to a new server: " + server.Name + "(" + server.Id + ")");

                if(!IsServerInitialised(server))
                    await LoadServerInformation(server);    
            }

        }

        private async Task ServerDisconnected(SocketGuild socketGuild)
        {
            _connectedServers.Remove(socketGuild);
        }

        private bool IsServerInitialised(SocketGuild server) {
            //TODO: Wait for initialising players
            foreach (MatchMakingServer myServer in _allServers) {
                if (myServer.DiscordServer.Id == server.Id)
                    return true;
            }

            return false;
        }

        private async Task BotOnMessageReceived(SocketMessage socketMessage) {
            foreach (var socketMessageMentionedUser in socketMessage.MentionedUsers)
            {
                if(socketMessageMentionedUser.Id == _bot.CurrentUser.Id)
                    await socketMessage.Channel.SendMessageAsync("Fuck you");
            }

        }

        /// <summary>
        /// Creates the commands.
        /// </summary>
        private async Task InitialiseCommands()
        {
            // TODO: AddCheck to all commands (if Server is not in _AllServers)
            // TODO: CheckIFRegistered should check if users is registered for the _correct_ server
            // TODO: CheckIFLeagueChannel check league channel for the _correct_ server
            //_commandMap.Add(_allServers);
            _commandMap.Add(_bot);
            _commandMap.Add(_allServers);
            _commandMap.Add(_databaseController);
            _bot.MessageReceived += HandleCommand;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos))) return;
            // Create a Command Context
            var context = new CommandContext(_bot, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed succesfully)
            var result = await _commands.ExecuteAsync(context, argPos, _commandMap);
            if (!result.IsSuccess)
                await message.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}
