using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace BanjoBot {
    class MyServer
    {
        public Server DiscordServer { get; set; }
        public List<LeagueController> LeagueController { get; set; }
        public List<League> Leagues { get; set; }

        public MyServer(Server discordServer)
        {
            
        }

        public MyServer(Server discordServer, List<League> leagues)
        {
            DiscordServer = discordServer;
            Leagues = leagues;
            foreach (League league in Leagues)
            {
                LeagueController.Add(new LeagueController(this,league));
            }
        }
    }
}
