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
    public class CommandModule : ModuleBase
    {
        private String test;
        private List<MatchMakingServer> _allServers;
        private DatabaseController _database;
        public CommandModule(List<MatchMakingServer> servers, DatabaseController databaseController)
        {
            _allServers = servers;
            _database = databaseController;
        }

        [Command("hostgame"), Summary("Creates a new game (only one game may be in the lobby at a time)."), Alias(new string[] { "host", "hg" })]
        public async Task Host() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            SocketGuildUser socketGuildUser = (SocketGuildUser)Context.User;
            if (lc.League.Role == null || socketGuildUser.RoleIds.Contains(lc.League.Role.Id)) {
                if (mms.IsGlobal() || lc.League.Channel == socketGuildChannel) {
                    await lc.TryHostGame(Context.Channel, player);
                }
            }
        }

        [Command("join"), Summary("Joins the open game."), Alias(new string[] { "j"})]
        public async Task Join() {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
            Player player = lc.League.GetPlayerByDiscordID(Context.User.Id);
            SocketGuildUser socketGuildUser = (SocketGuildUser)Context.User;
            if (lc.League.Role == null || socketGuildUser.RoleIds.Contains(lc.League.Role.Id)) {
                if (mms.IsGlobal() || lc.League.Channel == socketGuildChannel) {
                    await lc.JoinGame(Context.Channel, player);
                }
            }
        }

        [Command("register"), Summary("League registration")]
        public async Task Register([Summary("SteamID")]ulong steamid = 0 , [Summary("#Channel")]IChannel channel = null) {
            MatchMakingServer mms = GetMatchMakingServer(Context.Guild.Id);
            if (channel == null)
            {
                if (!mms.IsGlobal())
                {
                    await ReplyAsync("Join a league channel and try again or use #channel as a parameter");
                    return;
                }
            }

            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)Context.Channel;
            LeagueController lc = mms.GetLeagueController(socketGuildChannel);
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
                    
                }
                await lc.RegisterPlayer(player);
                //TODO: save to database
            }
            else
            {
                lc.League.ApplicantsIDs.Add(Context.User.Id);
                //TODO: save to database
                //TODO: add somewhere? modchannel?
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
        // Override the CheckPermissions method
        public override async Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo command,
            IDependencyMap map)
        {
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel) context.Channel;
            List<MatchMakingServer> servers = map.Get<List<MatchMakingServer>>();
            foreach (var matchMakingServer in servers)
            {
                if (matchMakingServer.DiscordServer.Id == context.Guild.Id)
                {
                    LeagueController lc = matchMakingServer.GetLeagueController(socketGuildChannel);
                    if (lc == null)
                    {
                        return PreconditionResult.FromError("Join a league channel and try again");
                    }

                    foreach (var player in lc.League.RegisteredPlayers)
                    {
                        if (player.User.Id == context.User.Id)
                        {
                            return PreconditionResult.FromSuccess();
                        }
                    }
                    return PreconditionResult.FromError("You are not registered. Sign up with !register <YourSteamID>");
                }
            }
            return PreconditionResult.FromError("Error: Server not found");
        }
    }

}
