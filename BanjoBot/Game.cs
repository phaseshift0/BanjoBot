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
        const int MAXPLAYERS = 8;
                
        // Props
        public String gameName          { get; set; }
        public User host                { get; set; }
        public List<User> waitingList   { get; set; }
        public List<User> redList       { get; set; }
        public List<User> blueList      { get; set; }
        public Teams winner             { get; set; }
        public List<User> redWinCalls   { get; set; }  
        public List<User> blueWinCalls  { get; set; }
        public List<User> drawCalls     { get; set; }
        public int mmrAdjustment        { get; set; }

        /// <summary>
        /// Game constructor. Queries database for game name and binds host to game.
        /// </summary>
        /// <param name="host">User who hosted the game.</param>
        public Game(User host)
        {
            this.gameName = "BBL#" + Program.ds.getGameCounter();
            this.host     = host;
            waitingList   = new List<User>();
            redList       = new List<User>();
            blueList      = new List<User>();
            blueWinCalls  = new List<User>();
            redWinCalls   = new List<User>();
            drawCalls     = new List<User>();
            waitingList.Add(host);
        }

        /// <summary>
        /// Adds a user to the game.
        /// </summary>
        /// <param name="user">User who wishes to join.</param>
        /// <returns>True if successful. False if game is full. Null if user already present.</returns>
        public bool? addPlayer(User user)
        {
            if (waitingList.Count == MAXPLAYERS)
                return false;
            else if (waitingList.Contains(user))
                return null;

            waitingList.Add(user);

            return true;
        }

        /// <summary>
        /// Removes User from game.
        /// </summary>
        /// <param name="user">User who wishes to leave.</param>
        /// <returns>True if sucessful. False if game is empty. Null if user not in game.</returns>
        public bool? removePlayer(User user)
        {
            if (!waitingList.Contains(user))
                return null;

            waitingList.Remove(user);

            if (waitingList.Count == 0)
                return false;

            if(user == host)
                host = waitingList.First();

            return true;
        }

        /// <summary>
        /// Starts the game.
        /// </summary>
        public String startGame(Channel textChannel, User user)
        {
            if (user.id != host.id)
                return "Not host";
            if (waitingList.Count < MAXPLAYERS)
                return "Not enough players";

            assignTeams();

            return generatePassword(6);
        }

        /// <summary>
        /// Assigns players in waiting list to teams of roughly equal MMR.
        /// By Michael Stimson.
        /// </summary>
        public void assignTeams()
        {
            var numPlayers = waitingList.Count;
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
                        mmr1 += waitingList[i].mmr;
                    else
                        mmr2 += waitingList[i].mmr;
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
                    redList.Add(waitingList[i]);
                else
                    blueList.Add(waitingList[i]);
            }

            // Remove the waiting list
            waitingList = null;
        }

        /// <summary>
        /// By Michael Stimson.
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
        /// Generates a random string of specified length using only alpha-numeric characters.
        /// </summary>
        /// <param name="length">Length of the string</param>
        /// <returns>String of random characters.</returns>
        public static String generatePassword(int length)
        {
            const String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new String(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Returns the average MMR of all players on the team.
        /// </summary>
        /// <param name="team">Can be either Blue or Red.</param>
        /// <returns>Average MMR as int.</returns>
        public int getTeamMMR(Teams team)
        {
            int averageMMR = 0;

            if(team == Teams.Red && redList.Count() > 0)
            {
                foreach (var user in redList)
                {
                    averageMMR += user.mmr;
                }
                averageMMR = averageMMR / redList.Count();
            }
            else if (team == Teams.Blue && blueList.Count() > 0)
            {
                foreach (var user in blueList)
                {
                    averageMMR += user.mmr;
                }
                averageMMR = averageMMR / blueList.Count();
            }

            return averageMMR;
        }
        
        /// <summary>
        /// Records a vote for the winning team.
        /// </summary>
        /// <param name="user">User who voted.</param>
        /// <param name="team">Team user voted for</param>
        /// <returns>String containing status of winning team.</returns>
        public String recordVote(User user, Teams team)
        {
            if (!redList.Contains(user) && !blueList.Contains(user))
                return "not in game";

            if (blueWinCalls.Contains(user) || redWinCalls.Contains(user))
                return "already voted";

            if (team == Teams.Blue)
            {
                blueWinCalls.Add(user);
                if (blueWinCalls.Count == 5)
                {
                    winner = Teams.Blue;
                    adjustStats(winner);
                    endGame();
                    return "blue";
                }
            }
            else if (team == Teams.Red)
            {
                redWinCalls.Add(user);
                if (redWinCalls.Count == 5)
                {
                    winner = Teams.Red;
                    adjustStats(winner);
                    endGame();
                    return "red";
                }
            }
            else if (team ==Teams.Draw)
            {
                drawCalls.Add(user);
                if (drawCalls.Count == 5)
                {
                    winner = Teams.Draw;
                    endGame();
                    return "draw";
                }
            }

            return "more votes";
        }

        /// <summary>
        /// Updates the Wins/Losses and MMR of all players in game depending on the winner.
        /// </summary>
        /// <param name="winner"></param>
        public void adjustStats(Teams winner)
        {
            if (winner == Teams.Blue)
            {
                double mmrDifference = getTeamMMR(Teams.Blue) - getTeamMMR(Teams.Red);

                int MMR = Convert.ToInt32(mmrCurve(mmrDifference));

                foreach (var user in blueList)
                {
                    user.wins++;
                    user.mmr += MMR + 2*user.streak;
                    user.streak++;
                }
                foreach (var user in redList)
                {
                    user.losses++;
                    user.streak = 0;
                    user.mmr -= MMR;
                    if (user.mmr < 0)
                        user.mmr = 0;
                }
            }

            if (winner == Teams.Red)
            {
                double mmrDifference = getTeamMMR(Teams.Red) - getTeamMMR(Teams.Blue);

                int MMR = Convert.ToInt32(mmrCurve(mmrDifference));

                foreach (var user in blueList)
                {
                    user.losses++;
                    user.streak = 0;
                    user.mmr -= MMR;
                    if (user.mmr < 0)
                        user.mmr = 0;
                }
                foreach (var user in redList)
                {
                    user.wins++;
                    user.mmr += MMR + 2 * user.streak;
                    user.streak++;
                }
            }
        }

        public double mmrCurve(double x)
        {
            double baseMMR = 25;
            double approaches = 10;
            double approachRate = Math.Atan(-x * (1/350.0));
            double result = approachRate * approaches + baseMMR;
            mmrAdjustment = Convert.ToInt32(result);
            return result;
        }

        /// <summary>
        /// Ends the game. Saves game data to DataBase.
        /// </summary>
        public void endGame()
        {
            if (redList.Count > 0 && blueList.Count > 0)
            {
                // Save DataStore data
                Program.ds.writeXML();
            }
        }
    }
}
