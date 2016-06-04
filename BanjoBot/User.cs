using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBot
{
    class User : IEquatable<User>      // Should extend Discord.User Class, just add mmr, wins and losses.
    {
        // Props
        public ulong id     { get; set; }
        public String name  { get; set; }
        public int mmr      { get; set; }
        public int wins     { get; set; }
        public int losses   { get; set; }
        public int streak   { get; set; }

        /// <summary>
        /// Create a new User and add it to the DataStore.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public User(ulong id, String name)
        {
            this.name = name;
            this.id = id;
            //var random = new Random();
            //mmr         = random.Next(1,1000);
            mmr = 1000;
            wins = 0;
            losses = 0;
            streak = 0;
        }

        /// <summary>
        /// Create a User controlling all of the variables. Used in readXML().
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="mmr"></param>
        /// <param name="wins"></param>
        /// <param name="losses"></param>
        public User(ulong id, String name, int mmr, int wins, int losses, int streak)
        {
            this.name = name;
            this.id = id;
            this.mmr = mmr;
            this.wins = wins;
            this.losses = losses;
            this.streak = streak;
        }

        /// <summary>
        /// Create a User controlling all of the variables. Used in readXML().
        /// Constructor without steak for upgrading existing XML file to be compatible with BanjoBot v1.1
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="mmr"></param>
        /// <param name="wins"></param>
        /// <param name="losses"></param>
        public User(ulong id, String name, int mmr, int wins, int losses)
        {
            this.name = name;
            this.id = id;
            this.mmr = mmr;
            this.wins = wins;
            this.losses = losses;
        }

        public bool Equals(User other)
        {
            return this.name == other.name &&
                   this.id == other.id &&
                   this.mmr == other.mmr &&
                   this.wins == other.wins &&
                   this.losses == other.losses &&
                   this.streak == other.streak;
        }

        public override string ToString()
        {
            return name + "(" + mmr + ")";
        }
    }
}
