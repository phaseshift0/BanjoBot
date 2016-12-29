using System.Collections.Generic;
using Discord;
using Discord.API;
using Discord.WebSocket;

namespace BanjoBot {
    public class League {
        public int LeagueID { get; set; }
        public string Name { get; set; } = "";
        public List<Player> RegisteredPlayers { get; set; }
        public List<ulong> ApplicantsIDs { get; set; } //TODO:
        public SocketGuildChannel Channel { get; set; }
        public SocketRole ModeratorRole { get; set;  }
        public SocketRole Role { get; set;  } 
        public int Season { get; set; }
        public int GameCounter { get; set; }
        public bool AutoAccept { get; set; } = true;
        public bool NeedSteamToRegister { get; set; } = true; 

        public League(int id, int season, SocketGuildChannel channel, SocketRole moderatorRole, int gameCounter = 0) {
            LeagueID = id;
            Season = season;
            Channel = channel;
            ModeratorRole = moderatorRole;
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
