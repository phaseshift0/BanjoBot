using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.API;
using Discord.Commands;
using Discord.WebSocket;

namespace BanjoBot {

    //TODO: IF server rdy
    //TODO: Move database access to leagueController
    //TODO: Move logic and controlling to .... controller
    //TODO: Refactor duplicated code
    //TODO: try catch throws database exceptions
    public class CommandModule : ModuleBase
    {
        private DiscordSocketClient _bot;
        private List<MatchMakingServer> _allServers;
        private DatabaseController _database;
        public CommandModule(List<MatchMakingServer> servers, DatabaseController databaseController, DiscordSocketClient bot)
        {
            _bot = bot;
            _allServers = servers;
            _database = databaseController;
        }

        [Command("hostgame"), Summary("Creates a new game (only one game may be in the lobby at a time)."), Alias(new string[] { "host", "hg" }), RequireLeaguePermission]
        public async Task Host() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            await lc.TryHostGame(Context.Channel, player);
        }

        [Command("join"), Summary("Joins the open game."), Alias(new string[] { "j"}), RequireLeaguePermission]
        public async Task Join() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            await lc.JoinGame(Context.Channel, player);  
        }

        [Command("leave"), Summary("Leaves the open game."), Alias(new string[] { "l" }), RequireLeaguePermission]
        public async Task Leave() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            await lc.LeaveGame(Context.Channel, player);
        }

        [Command("cancel"), Summary("Cancel the current lobby"), Alias(new string[] { "c" }), RequireLeaguePermission]
        public async Task CancelGame() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            await lc.CancelGame(Context.Channel,player);
        }

        [Command("votecancel"), Summary("Casts a vote to cancel the open game."), Alias(new string[] { "vc" }), RequireLeaguePermission]
        public async Task VoteCancel() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            await lc.VoteCancel(Context.Channel, player);
        }

        [Command("startgame"), Summary("Start the game. Host only, requires full game."), Alias(new string[] { "sg" }), RequireLeaguePermission]
        public async Task StartGame() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            await lc.StartGame(Context.Channel, player);
        }

        [Command("getplayers"), Summary("Shows the players that have joined the open game."), Alias(new string[] { "players","list" }), RequireLeagueChannel]
        public async Task GetPlayers() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            await lc.ListPlayers(Context.Channel);
        }

        [Command("showstats"), Summary("Shows the league stats of a player."), Alias(new string[] { "stats", "gs" }), RequireLeaguePermission]
        public async Task ShowStats([Summary("@Player")]IGuildUser guildUser = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = null;
            if (guildUser == null)
            {
                player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            }
            else
            {
                player = lc.League.GetPlayerByDiscordID(guildUser.Id);
            }
            await lc.GetStats(Context.Channel, player);
        }

        [Command("getgames"), Summary("Shows the status of all games."), Alias(new string[] { "gg", "games" }), RequireLeagueChannel]
        public async Task ShowGames() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            await lc.ShowGames(Context.Channel);
        }


        [Command("bluewins"), Summary("Cast vote for Blue Team as the winner of game BBL#X (post game only)."), Alias(new string[] { "blue", "bw" }), RequireLeaguePermission]
        public async Task VoteBlue() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            await lc.VoteWinner(Context.Channel,player,Teams.Blue);
        }

        [Command("redwins"), Summary("Cast vote for Red Team as the winner of game BBL#X (post game only)."), Alias(new string[] { "red", "rw" }), RequireLeaguePermission]
        public async Task VoteRed() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            await lc.VoteWinner(Context.Channel, player, Teams.Red);
        }

        [Command("draw"), Summary("Cast vote for Red Team as the winner of game BBL#X (post game only)."), Alias(new string[] { "d"}), RequireLeaguePermission]
        public async Task VoteDraw() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            await lc.VoteWinner(Context.Channel, player, Teams.Draw);
        }

        [Command("topmmr"), Summary("Shows the status of all games."), Alias(new string[] { "top", "t" }), RequireLeagueChannel]
        public async Task TopMMR() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            await lc.ShowTopMMR(Context.Channel);
        }

        [Command("register"), Summary("League registration"), RequireLeagueChannel]
        public async Task Register([Summary("SteamID")]ulong steamid = 0 , [Summary("#Channel")]IChannel channel = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);

            //TODO: use current channel if null!
            //TODO: check parameter order
            SocketGuildChannel socketGuildChannel = null;
            if (channel != null) {
                socketGuildChannel = (SocketGuildChannel)channel;
            }
            else {
                socketGuildChannel = (SocketGuildChannel)Context.Channel;
            }
            
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            foreach (var player in lc.League.RegisteredPlayers)
            {
                if (player.User.Id == Context.User.Id)
                {
                    await ReplyAsync("You are already registered");
                    return;
                }
            }
            if (lc.League.ApplicantsIDs.Contains(Context.User.Id))
            {
                await ReplyAsync("You are already signed up, wait for the approval by a moderator");
                return;
            }

            if (lc.League.NeedSteamToRegister && steamid == 0)
            {
                await ReplyAsync("Missing steamID. Please use !register YourSteamID");
                return;
                
            }

            if (lc.League.AutoAccept)
            {
                Player player = mms.GetPlayer(Context.User.Id);
                if (player == null)
                {
                    player = new Player((SocketGuildUser) Context.User, steamid);
                    await _database.InsertNewPlayer(player);

                }
                await lc.RegisterPlayer(player);
                await _database.RegisterPlayerToLeague(player, lc.League);
                await _database.UpdatePlayerStats(player, player.GetLeagueStat(lc.League.LeagueID));
                await ReplyAsync("You are registered now. Use !help to see the command list");
            }
            else
            {
                lc.League.ApplicantsIDs.Add(Context.User.Id);
                await _database.InsertSignupToLeague(Context.User.Id, lc.League);
                await ReplyAsync("You are signed up now. Wait for the approval by a moderator");
                //TODO: add somewhere? modchannel?
            }


        }


        // Admin commands


        [Command("autoaccept"), Summary("Sets registration to automatic"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetAutoAccept([Summary("True/False")]bool  autoAccept, [Summary("#Channel")]IChannel channel = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            if (channel != null) {
                socketGuildChannel = (SocketGuildChannel)channel;
            }
            else {
                socketGuildChannel = (SocketGuildChannel)Context.Channel;
            }
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            if (lc == null)
            {
                await ReplyAsync("Join a league channel first or pass a channel as a parameter #channel.");
                return;
            }

            lc.League.AutoAccept = autoAccept;
            await _database.UpdateLeague(lc.League);
        }

        [Command("steamregister"), Summary("Sets registration to automatic"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetSteamRegister([Summary("True/False")]bool steamregister, [Summary("#Channel")]IChannel channel = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            if (channel != null) {
                socketGuildChannel = (SocketGuildChannel)channel;
            }
            else {
                socketGuildChannel = (SocketGuildChannel)Context.Channel;
            }
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            if (lc == null) {
                await ReplyAsync("Join a league channel first or pass a channel as a parameter #channel.");
                return;
            }

            lc.League.NeedSteamToRegister = steamregister;
            await _database.UpdateLeague(lc.League);
        }

        [Command("createleague"), Summary("Creates a league"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateLeague([Summary("#Channel")]IChannel channel = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = null;
            if (channel != null)
            {
                socketGuildChannel = (SocketGuildChannel)channel;
            }
            else
            {
                 socketGuildChannel = (SocketGuildChannel)Context.Channel;
            }

            if (mms.IsGlobal())
            {
                LeagueController lc = mms.GetLeagueController(socketGuildChannel);
                lc.League.Channel = socketGuildChannel;
                await _database.UpdateLeague(lc.League);
                await ReplyAsync("Global league disabled. League assigned to channel " + socketGuildChannel.Name);
            }
            else
            {
                LeagueController lc = mms.GetLeagueController(socketGuildChannel);
                if (lc != null) {
                    await ReplyAsync("This channel is already assigned to another league.");
                }
                else 
                {
                    int leagueID = await _database.InsertNewLeague(Context.Guild.Id);
                    League league = new League(leagueID,1,socketGuildChannel,null);
                    mms.AddLeague(league);
                    await _database.UpdateLeague(league);
                    await ReplyAsync("League created.");
                }
            }
        }

        [Command("deleteleague"), Summary("Deletes a league"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteLeague([Summary("#Channel")]IChannel channel = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = null;
            if (channel != null) {
                socketGuildChannel = (SocketGuildChannel)channel;
            }
            else {
                socketGuildChannel = (SocketGuildChannel)Context.Channel;
            }

            if (mms.IsGlobal()) {
                await ReplyAsync("There is only a global league. Set a channel for the league with the command setchannel #channel");
                return;
            }

            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            if (lc != null)
            {
                await _database.DeleteLeague(lc.League);
                mms.DeleteLeague(lc);
                await ReplyAsync("League deleted.");
            }
            else {
                await ReplyAsync("This channel is not assigned to league.");
            }
            
        }

        [Command("setchannel"), Summary("Sets the league channel"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetChannel([Summary("#Channel")]IChannel channel = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = null;
            if (channel != null) {
                socketGuildChannel = (SocketGuildChannel)channel;
            }
            else {
                socketGuildChannel = (SocketGuildChannel)Context.Channel;
            }

            if (mms.IsGlobal()) {
                LeagueController lc = mms.GetLeagueController(socketGuildChannel);
                lc.League.Channel = socketGuildChannel;
                await _database.UpdateLeague(lc.League);
                await ReplyAsync("Global league disabled. League assigned to channel " + socketGuildChannel.Name);
            }
            else {
                LeagueController lc = mms.GetLeagueController(socketGuildChannel);
                if (lc == null) {
                    await ReplyAsync("This is no league channel.");
                }
                else
                {
                    lc.League.Channel = socketGuildChannel;
                    await _database.UpdateLeague(lc.League);
                    await ReplyAsync("League updated.");
                }
            }
        }

        [Command("setrole"), Summary("Sets the league role"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetRole([Summary("@Role")]IRole role = null, [Summary("#Channel")]IChannel channel = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = null;
            if (channel != null) {
                socketGuildChannel = (SocketGuildChannel)channel;
            }
            else {
                socketGuildChannel = (SocketGuildChannel)Context.Channel;
            }

            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            if (lc == null) {
                await ReplyAsync("This is no league channel.");
            }
            else {
                
                foreach (var player in lc.League.RegisteredPlayers) {
                    if (role == null && lc.League.Role != null)
                    {
                        await player.User.RemoveRolesAsync(lc.League.Role);
                    }
                    else
                    {
                        await player.User.AddRolesAsync((SocketRole)role);
                    }
                   
                }
                lc.League.Role = (SocketRole)role;
                await _database.UpdateLeague(lc.League);
                if (role == null) {
                    await ReplyAsync("League role deleted.");
                }
                else {
                    await ReplyAsync("League role assigned. New Role: " + role.Mention);
                }
            }
            
        }

        [Command("listleagues"), Summary("Shows all leagues"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ListLeagues() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
          

            object[] args = new object[] {"ID", "Name",
                    "Channel", "Role", "AutoAccept", "Steam",
                    "Season", "Matches", "Players",
                    "Applicants"};
            String s = String.Format(
                        "{0,-4} {1,-10} {2,-10} {3,-10} {4,-12} {5,-8} {6,-8} {7,-8} {8,-8} {9,-10}\n", args);
            foreach (var lc in mms.LeagueController)
            {
                string channel = lc.League.Channel != null ? lc.League.Channel.Name : "none";
                string role = lc.League.Role != null ? lc.League.Role.Name : "none";
                args = new object[] {lc.League.LeagueID, lc.League.Name,
                    channel, role, lc.League.AutoAccept, lc.League.NeedSteamToRegister,
                    lc.League.Season, lc.League.GameCounter, lc.League.RegisteredPlayers.Count,
                    lc.League.ApplicantsIDs.Count};
                s += String.Format("{0,-4} {1,-10} {2,-10} {3,-12} {4,-10} {5,-8} {6,-8} {7,-8} {8,-8} {9,-10}\n", args);
            }
            await ReplyAsync("```" + s + "```");

        }


        [Command("startseason"), Summary("Ends current season and starts a new season"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task StartSeason([Summary("#Channel")]IChannel channel = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = null;
            if (channel != null) {
                socketGuildChannel = (SocketGuildChannel)channel;
            }
            else {
                socketGuildChannel = (SocketGuildChannel)Context.Channel;
            }

            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            if (lc == null) {
                await ReplyAsync("This is no league channel.");
            }
            else
            {
                IMessageChannel repsondChannel;
                if (lc.League.Channel != null)
                {
                    repsondChannel = (IMessageChannel) lc.League.Channel;
                }
                else
                {
                    repsondChannel = (IMessageChannel) socketGuildChannel;
                }
                await repsondChannel.SendMessageAsync("Season " + lc.League.Season + " has ended.");
                await repsondChannel.SendMessageAsync("Top Players Season " + lc.League.Season + ": ");
                var sortedDict = from entry in lc.League.RegisteredPlayers orderby entry.GetMMR(lc.League.LeagueID) descending select entry;
                object[] args = new object[] { "Name", "MMR", "Matches", "Wins", "Losses"};
                String s = String.Format("{0,-10} {1,-6} {2,-10} {3,-12} {4,-10}\n", args);
                foreach (var player in sortedDict)
                {
                    LeagueStats stats = player.GetLeagueStat(lc.League.LeagueID);
                    args = new object[] { player.User.Username, stats.MMR, stats.Matches, stats.Wins, stats.Losses };
                    s += String.Format("{0,-10} {1,-6} {2,-10} {3,-12} {4,-10}\n", args);

                    stats.ResetToSeason(lc.League.Season + 1);
                    await _database.UpdatePlayerStats(player, stats);
                }
                
                await repsondChannel.SendMessageAsync("```" + s + "```");
                lc.League.Season++;
                await _database.UpdateLeague(lc.League);
            }
        }


        [Command("hey"), Summary("Echos a message.")]
        public async Task Say() {
            await ReplyAsync("Shup");
        }

        public MatchMakingServer GetMatchMakingServer(ulong serverid) {
            foreach (MatchMakingServer myServer in _allServers) {
                if (myServer.DiscordServer.Id == serverid)
                    return myServer;
            }

            return null;
        }
    }

    public class RequireLeaguePermission : PreconditionAttribute {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map) {
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)context.Channel;
            List<MatchMakingServer> servers = map.Get<List<MatchMakingServer>>();
            foreach (var matchMakingServer in servers) {
                if (matchMakingServer.DiscordServer.Id == context.Guild.Id) {
                    LeagueController lc = matchMakingServer.GetLeagueController(socketGuildChannel);
                    if (lc == null) {
                        return PreconditionResult.FromError("Join a league channel and try again");
                    }

                    foreach (var player in lc.League.RegisteredPlayers) {
                        if (player.User.Id == context.User.Id) {
                            return PreconditionResult.FromSuccess();
                        }
                    }
                    return PreconditionResult.FromError("You are not registered. Sign up with !register <YourSteamID>");
                }
            }
            return PreconditionResult.FromError("Error: Server not found");
        }
    }


    public class RequireLeagueChannel : PreconditionAttribute {

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map) {
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)context.Channel;
            List<MatchMakingServer> servers = map.Get<List<MatchMakingServer>>();
            foreach (var matchMakingServer in servers) {
                if (matchMakingServer.DiscordServer.Id == context.Guild.Id) {
                    LeagueController lc = matchMakingServer.GetLeagueController(socketGuildChannel);
                    if (lc == null) {
                        return PreconditionResult.FromError("Join a league channel and try again");
                    }
                    else {
                        return PreconditionResult.FromSuccess();
                    }
                }
            }
            return PreconditionResult.FromError("Error: Server not found");
        }
    }

}
