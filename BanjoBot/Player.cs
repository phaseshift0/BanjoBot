using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.API;
using Discord.WebSocket;

namespace BanjoBot
{
    public class Player : IEquatable<Player> {

        // TODO: remove getter setter and replace with GetLeagueStats(Leagueid)
        public SocketGuildUser User { get; set; }
        public ulong SteamID { get; set; }
        public Game CurrentGame   { get; set; }
        public List<LeagueStats> LeagueStats { get; set; }

        public Player(SocketGuildUser discordUser, ulong steamid)
        {
            User = discordUser;
            SteamID = steamid;
            CurrentGame = null;
            LeagueStats = new List<LeagueStats>();
        }

        public bool Equals(Player other)
        {
            return User.Id == other.User.Id;
        }

        public string PlayerMMRString(int leagueID)
        {
            return User.Username + "(" + GetLeagueStat(leagueID).MMR + ")";
        }

        private LeagueStats GetLeagueStat(int leagueID) {
            foreach (var leagueStat in LeagueStats) {
                if (leagueStat.LeagueID == leagueID)
                    return leagueStat;
            }
            return null;
        }

        public int GetMMR(int leagueID)
        {
            return GetLeagueStat(leagueID).MMR;
        }

        public int GetWins(int leagueID) 
            {
            return GetLeagueStat(leagueID).Wins;
        }

        public int GetLosses(int leagueID) 
            {
            return GetLeagueStat(leagueID).Losses;
        }

        public int GetStreak(int leagueID) 
            {
            return GetLeagueStat(leagueID).Streak;
        }

        public int GetMatches(int leagueID)
        {
            return GetLeagueStat(leagueID).Matches;
        }

        public void IncMatches(int leagueID)
        {
            GetLeagueStat(leagueID).Matches++;
        }

        public void IncMMR(int leagueID, int mmr) {
            GetLeagueStat(leagueID).MMR += mmr;
        }

        public void SetMMR(int leagueID, int mmr) {
            GetLeagueStat(leagueID).MMR = mmr;
        }

        public void DecMMR(int leagueID, int mmr) {
            GetLeagueStat(leagueID).MMR -= mmr;
        }

        public void IncWins(int leagueID)
        {
            GetLeagueStat(leagueID).Wins++;
        }

        public void IncLosses(int leagueID)
        {
            GetLeagueStat(leagueID).Losses++;
        }
        public void IncStreak(int leagueID)
        {
            GetLeagueStat(leagueID).Streak++;
        }
        public void SetStreakZero(int leagueID) {
            GetLeagueStat(leagueID).Streak = 0;
        }
        public bool IsIngame()
        {
            return CurrentGame != null;
        }

    }
}
