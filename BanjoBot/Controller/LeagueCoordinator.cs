using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.API;
using Discord.API.Rest;
using Discord.WebSocket;

namespace BanjoBot {
    public class LeagueCoordinator
    {
        private static readonly LeagueCoordinator instance = new LeagueCoordinator();
        public List<LeagueController> LeagueController { get; set; }

        static LeagueCoordinator() {}

        private LeagueCoordinator()
        {
            LeagueController = new List<LeagueController>();
        }

        public static LeagueCoordinator Instance
        {
            get { return instance; }
        }

        public void AddLeague(League league)
        {
            LeagueController.Add(new LeagueController(this,league));
        }

        public void AddLeague(List<League> leagues) {
            foreach (var league in leagues)
            {
                AddLeague(league);
            }
        }

        public void DeleteLeague(LeagueController league) {
            LeagueController.Remove(league);
        }

        public LeagueController GetLeagueController(SocketGuildChannel channel)
        {
            foreach (LeagueController leagueController in LeagueController)
            {
                if (leagueController.League.DiscordInformation != null && leagueController.League.DiscordInformation.DiscordServer != null)
                {
                    if(leagueController.League.DiscordInformation.Channel == channel)
                        return leagueController;
                }
            }

            return null;
        }

        public List<LeagueController> GetLeagueControllersByServer(SocketGuild guild)
        {
            List<LeagueController> result = new List<LeagueController>();
            foreach (LeagueController leagueController in LeagueController) {
                if (leagueController.League.DiscordInformation != null && leagueController.League.DiscordInformation.DiscordServerId == guild.Id) {
                    result.Add(leagueController);
                }
            }
            return result;
        }

        public LeagueController GetLeagueController(int LeagueID) {
            foreach (LeagueController leagueController in LeagueController) {
                if (leagueController.League.LeagueID == LeagueID) {
                    return leagueController;
                }
            }

            return null;
        }

        public Player GetPlayerByDiscordID(ulong userID) {
            foreach (var lc in LeagueController)
            {
                foreach (var regplayer in lc.League.RegisteredPlayers)
                {
                    if (regplayer.discordID == userID) {
                        return regplayer;
                    }
                }
            }
         
            return null;
        }


        public Player GetPlayerBySteamID(ulong steamID) {
            foreach (var lc in LeagueController) {
                foreach (var regplayer in lc.League.RegisteredPlayers) {
                    if (regplayer.SteamID == steamID) {
                        return regplayer;
                    }
                }
            }

            return null;
        }
    }
}
