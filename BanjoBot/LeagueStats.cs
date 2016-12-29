using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBot {
    public class LeagueStats {
        public int LeagueID { get; set; }
        public int Season { get; set; }
        public int Matches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int MMR { get; set; }
        public int Streak { get; set; }

        public LeagueStats(int leagueID, int season, int matches = 0, int wins = 0, int losses = 0, int mmr = 1000, int streak = 0)
        {
            LeagueID = leagueID;
            Season = season;
            Matches = matches;
            Wins = wins;
            Losses = losses;
            MMR = mmr;
            Streak = streak;
        }

        public void ResetToSeason(int season)
        {
            Season = season;
            Matches = 0;
            Wins = 0;
            Losses = 0;
            MMR = 1000;
            Streak = 0;
        }
    }
}
