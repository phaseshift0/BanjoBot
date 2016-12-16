using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBot
{
    /// <summary>
    /// Enumerator for referring to teams.
    /// </summary>
    public enum Teams { Red, Blue, Draw };

    class Game
    {
        // Constants
        public const int MAXPLAYERS    = 8;
        public const int VOTETHRESHOLD = 5;
                
        // Props
        public String GameName          { get; set; }
        public Player Host                { get; set; }
        public List<Player> WaitingList   { get; set; }
        public List<Player> RedList       { get; set; }
        public List<Player> BlueList      { get; set; }
        public Teams Winner             { get; set; }
        public List<Player> CancelCalls   { get; set; }
        public List<Player> RedWinCalls   { get; set; }  
        public List<Player> BlueWinCalls  { get; set; }
        public List<Player> DrawCalls     { get; set; }
        public bool HasStarted { get; set; }
        public int MmrAdjustment        { get; set; }

        /// <summary>
        /// Game constructor. Queries database for game name and binds Host to game.
        /// </summary>
        /// <param name="host">User who hosted the game.</param>
        public Game(Player host, int gameNumber)
        {
            GameName = "BBL#" + gameNumber;
            Host     = host;
            HasStarted = false;
            WaitingList   = new List<Player>();
            RedList       = new List<Player>();
            BlueList      = new List<Player>();
            CancelCalls   = new List<Player>();
            BlueWinCalls  = new List<Player>();
            RedWinCalls   = new List<Player>();
            DrawCalls     = new List<Player>();
            WaitingList.Add(host);
        }

        /// <summary>
        /// Adds a player to the game.
        /// </summary>
        /// <param name="player">User who wishes to join.</param>
        /// <returns>True if successful. False if game is full. Null if player already present.</returns>
        public bool? addPlayer(Player player)
        {
            if (WaitingList.Count == MAXPLAYERS)
                return false;
            else if (WaitingList.Contains(player))
                return null;

            WaitingList.Add(player);

            return true;
        }

        /// <summary>
        /// Removes User from game.
        /// </summary>
        /// <param name="user">User who wishes to leave.</param>
        /// <returns>True if sucessful. False if game is empty. Null if player not in game.</returns>
        public bool? removePlayer(Player user)
        {
            if (!WaitingList.Contains(user))
                return null;

            WaitingList.Remove(user);

            if (WaitingList.Count == 0)
                return false;

            if(user == Host)
                Host = WaitingList.First();

            return true;
        }

        /// <summary>
        /// Starts the game.
        /// </summary>
        public void startGame()
        {
            assignTeams();
            HasStarted = true;
        }

        /// <summary>
        /// Assigns players in waiting list to teams of roughly equal MMR.
        /// By Kael
        /// </summary>
        public void assignTeams()
        {
            var numPlayers = WaitingList.Count;
            var mmrs = new List<int>();
            var subsets = tryCombinations(numPlayers);
            var storedTeams = new List<int>();
            var bestMmrDiff = double.PositiveInfinity;

            foreach (var s in subsets)
            {
                var mmr1 = 0;
                var mmr2 = 0;

                for (int i=0; i<numPlayers; i++)
                {
                    if (s[i] == 1)
                        mmr1 += WaitingList[i].Mmr;
                    else
                        mmr2 += WaitingList[i].Mmr;
                }

                var difference = Math.Abs(mmr1 - mmr2);

                if (difference < bestMmrDiff)
                {
                    bestMmrDiff = difference;
                    storedTeams = s;
                }
            }

            // Assign the players to teams
            for (int i=0; i<numPlayers; i++)
            {
                if (storedTeams[i] == 1)
                    RedList.Add(WaitingList[i]);
                else
                    BlueList.Add(WaitingList[i]);
            }
        }

        /// <summary>
        /// By Kael
        /// </summary>
        /// <param name="numPlayers">Number of players in the game.</param>
        /// <returns></returns>
        public List<List<int>> tryCombinations(int numPlayers)
        {
            var output = new List<List<int>>();
            output.Add(new List<int>());
            output.Add(new List<int>());
            output[0].Add(1);
            output[1].Add(0);

            for (int i = 1; i < numPlayers; i++)
            {
                var count = output.Count;
                for (int oIndex = 0; oIndex < count; oIndex++)
                {
                    var o = output[oIndex];
                    var copy = o.Select(v => v).ToList();
                    o.Add(0);
                    copy.Add(1);
                    output.Add(copy);
                }
            }

            var freshOutput = new List<List<int>>();

            foreach (var o in output)
            {
                var sum = o.Sum();
                if (sum < numPlayers / 2 || sum > (numPlayers / 2) + 1)
                    continue;
                freshOutput.Add(o);
            }

            return freshOutput;
        }

        /// <summary>
        /// Returns the average MMR of all players on the team.
        /// </summary>
        /// <param name="team">Can be either Blue or Red.</param>
        /// <returns>Average MMR as int.</returns>
        public int getTeamMMR(Teams team)
        {
            int averageMMR = 0;

            if(team == Teams.Red && RedList.Count() > 0)
            {
                foreach (var user in RedList)
                {
                    averageMMR += user.Mmr;
                }
                averageMMR = averageMMR / RedList.Count();
            }
            else if (team == Teams.Blue && BlueList.Count() > 0)
            {
                foreach (var user in BlueList)
                {
                    averageMMR += user.Mmr;
                }
                averageMMR = averageMMR / BlueList.Count();
            }

            return averageMMR;
        }
        
        /// <summary>
        /// Records votes to cancel game. Once the number of votes reaches the VOTETHREASHOLD, the game is canceled.
        /// </summary>
        /// <param name="user">User who voted.</param>
        /// <returns>String indicating status</returns>
        public String voteCancel(Player user)
        {
            if (!WaitingList.Contains(user))
                return "not in game";
            if (CancelCalls.Contains(user))
                return "already voted";

            CancelCalls.Add(user);

            if (CancelCalls.Count >= Math.Floor((double)WaitingList.Count()/2))
                return "canceled";

            return "more votes";
        }

        /// <summary>
        /// Records a vote for the winning team.
        /// </summary>
        /// <param name="user">User who voted.</param>
        /// <param name="team">Team player voted for</param>
        /// <returns>String containing status of winning team.</returns>
        public String recordVote(Player user, Teams team)
        {
            if (!RedList.Contains(user) && !BlueList.Contains(user))
                return "not in game";

            if (team == Teams.Blue)
            {
                if (BlueWinCalls.Contains(user))
                    return "already voted";
                if (RedWinCalls.Contains(user))
                    RedWinCalls.Remove(user);
                if (DrawCalls.Contains(user))
                    DrawCalls.Remove(user);
                BlueWinCalls.Add(user);
                if (BlueWinCalls.Count == VOTETHRESHOLD)
                {
                    Winner = Teams.Blue;
                    AdjustStats(Winner);
                    endGame();
                    return "blue";
                }
            }
            else if (team == Teams.Red)
            {
                if (RedWinCalls.Contains(user))
                    return "already voted";
                if (BlueWinCalls.Contains(user))
                    BlueWinCalls.Remove(user);
                if (DrawCalls.Contains(user))
                    DrawCalls.Remove(user);
                RedWinCalls.Add(user);
                if (RedWinCalls.Count == VOTETHRESHOLD)
                {
                    Winner = Teams.Red;
                    AdjustStats(Winner);
                    endGame();
                    return "red";
                }
            }
            else if (team ==Teams.Draw)
            {
                if (DrawCalls.Contains(user))
                    return "already voted";
                if (BlueWinCalls.Contains(user))
                    BlueWinCalls.Remove(user);
                if (RedWinCalls.Contains(user))
                    RedWinCalls.Remove(user);
                DrawCalls.Add(user);
                if (DrawCalls.Count == VOTETHRESHOLD)
                {
                    Winner = Teams.Draw;
                    endGame();
                    return "draw";
                }
            }

            return "more votes";
        }

        /// <summary>
        /// Updates the Wins/Losses and MMR of all players in game depending on the Winner.
        /// </summary>
        /// <param name="winner"></param>
        public void AdjustStats(Teams winner)
        {
            if (winner == Teams.Blue)
            {
                double mmrDifference = getTeamMMR(Teams.Blue) - getTeamMMR(Teams.Red);

                int MMR = Convert.ToInt32(mmrCurve(mmrDifference));

                foreach (var user in BlueList)
                {
                    user.Wins++;
                    user.Mmr += MMR + 2*user.Streak;
                    user.Streak++;
                }
                foreach (var user in RedList)
                {
                    user.Losses++;
                    user.Streak = 0;
                    user.Mmr -= MMR;
                    if (user.Mmr < 0)
                        user.Mmr = 0;
                }
            }

            if (winner == Teams.Red)
            {
                double mmrDifference = getTeamMMR(Teams.Red) - getTeamMMR(Teams.Blue);

                int MMR = Convert.ToInt32(mmrCurve(mmrDifference));

                foreach (var user in BlueList)
                {
                    user.Losses++;
                    user.Streak = 0;
                    user.Mmr -= MMR;
                    if (user.Mmr < 0)
                        user.Mmr = 0;
                }
                foreach (var user in RedList)
                {
                    user.Wins++;
                    user.Mmr += MMR + 2 * user.Streak;
                    user.Streak++;
                }
            }
        }

        public double mmrCurve(double x)
        {
            double baseMMR = 25;
            double approaches = 10;
            double approachRate = Math.Atan(-x * (1/350.0));
            double result = approachRate * approaches + baseMMR;
            MmrAdjustment = Convert.ToInt32(result);
            return result;
        }

        /// <summary>
        /// Ends the game. Saves game data to DataBase.
        /// </summary>
        public void endGame()
        {
            if (RedList.Count > 0 && BlueList.Count > 0)
            {
                // Save DataStore data
                Program.ds.writeXML();
            }
        }

        /// <summary>
        /// Generates a random string of specified length using only alpha-numeric characters.
        /// </summary>
        /// <param name="length">Length of the string</param>
        /// <returns>String of random characters.</returns>
        public static String GeneratePassword(int length) {
            const String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new String(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
