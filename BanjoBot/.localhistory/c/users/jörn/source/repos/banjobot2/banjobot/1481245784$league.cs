using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBot {
    class League {
        private Game _activeGame;
        private List<Game> _runningGames;
        public Channel Channel { get; set; }
        public Role Role { get; set; }
        public String Name { get; set; }

        public League(String name, Channel channel) {
            Channel = channel;
            Name = name;
        }

        public League(String name, Channel channel, Role role) : this(name, channel)
        {
            Role = role;
        }
    }
}
