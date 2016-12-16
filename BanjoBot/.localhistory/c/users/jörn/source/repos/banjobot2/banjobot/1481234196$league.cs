using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBot {
    class League {
        private Game activeGame;
        private List<Game> runningGames;
        public Channel channel { get; set; }
        public Role role { get; set; }
        public String name { get; set; }

        public League(Channel channel) {
            this.channel = channel;
        }
    }
}
