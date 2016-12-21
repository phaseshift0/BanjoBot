using System.Collections.Generic;
using Discord;
using Discord.API;
using Discord.WebSocket;

namespace BanjoBot {
    public class League {
        //TODO: ActiveGame and RunningGames to LC
        public int LeagueID { get; set; }
        public string Name { get; set; } //TODO:
        public List<Player> RegisteredPlayers { get; set; }
        public List<ulong> ApplicantsIDs { get; set; } //TODO:
        public SocketGuildChannel Channel { get; set; }
        public SocketRole Role { get; set; }
        public int Season { get; set; }
        public int GameCounter { get; set; } //TODO:
        public bool AutoAccept { get; set; } = true; //TODO:
        public bool NeedSteamToRegister { get; set; } = true; //TODO:

        public League(int id, int season, SocketGuildChannel channel, SocketRole role, int gameCounter = 0) {
            LeagueID = id;
            Season = season;
            Channel = channel;
            Role = role;
            GameCounter = gameCounter;
            RegisteredPlayers = new List<Player>();
            ApplicantsIDs = new List<ulong>();
        }

        public Player GetPlayerByDiscordID(ulong id)
        {
            foreach (Player player in RegisteredPlayers)
            {
                if (player.User.Id == id)
                    return player;
            }
            return null;
        }
    }
}
