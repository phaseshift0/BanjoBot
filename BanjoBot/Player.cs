using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBot
{
    class Player : IEquatable<Player>
    {
        // Props
        public User User { get; set; }
        public ulong ID             { get; set; }
        public String Name          { get; set; }
        public String Mention       { get; set; }
        public int Mmr              { get; set; }
        public int Wins             { get; set; }
        public int Losses           { get; set; }
        public int Streak           { get; set; }
        public Game CurrentGame   { get; set; }

        /// <summary>
        /// Create a new User and add it to the DataStore.
        /// </summary>
        /// <param Name="id"></param>
        /// <param Name="name"></param>
        public Player(ulong id, String name, String mention, User user)
        {
            User = user;
            Name    = name;
            ID      = id;
            Mention = mention;
            Mmr          = 1000;
            Wins         = 0;
            Losses       = 0;
            Streak       = 0;
        }

        /// <summary>
        /// Create a User controlling all of the variables. Used in readXML().
        /// </summary>
        /// <param Name="id"></param>
        /// <param Name="name"></param>
        /// <param Name="Mmr"></param>
        /// <param Name="Wins"></param>
        /// <param Name="Losses"></param>
        public Player(ulong id, String name, String mention, int mmr, int wins, int losses, int streak)
        {
            ID      = id;
            Name    = name;
            Mention = mention;
            Mmr     = mmr;
            Wins    = wins;
            Losses  = losses;
            Streak  = streak;
        }

        /// <summary>
        /// Create a User controlling all of the variables. Used in readXML().
        /// Constructor without Mention for upgrading existing XML file to be compatible with BanjoBot v1.2
        /// </summary>
        /// <param Name="id"></param>
        /// <param Name="name"></param>
        /// <param Name="Mmr"></param>
        /// <param Name="Wins"></param>
        /// <param Name="Losses"></param>
        public Player(ulong id, String name, int mmr, int wins, int losses, int streak)
        {
            Name = name;
            ID = id;
            Mmr = mmr;
            Wins = wins;
            Losses = losses;
            Streak = streak;
        }

        public bool Equals(Player other)
        {
            return ID == other.ID &&
                   Name == other.Name &&
                   Mention == other.Mention &&
                   Mmr == other.Mmr &&
                   Wins == other.Wins &&
                   Losses == other.Losses &&
                   Streak == other.Streak;
        }

        public override string ToString()
        {
            return Name + "(" + Mmr + ")";
        }


        public bool IsIngame()
        {
            return CurrentGame != null;
        }
    }
}
