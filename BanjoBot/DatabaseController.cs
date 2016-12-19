using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MySql.Data.MySqlClient;

namespace BanjoBot {
    class DatabaseController {
        private MySql.Data.MySqlClient.MySqlConnection _connection;

        public DatabaseController()
        {
            string myConnectionString = "server=127.0.0.1;uid=banjo_admin;" + "pwd=D2bblXX!;database=banjoball;";
            _connection = new MySql.Data.MySqlClient.MySqlConnection();
            _connection.ConnectionString = myConnectionString;
        }

        public void InsertMatch(Game game)
        {
            
        }

        public void UpdateMatchResult(Game game)
        {
            
        }

        public void InsertLeague()
        {
            
        }

        public void UpdateLeague() 
        {

        }

        public int InsertNewLeague(ulong discordServerID)
        {
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText = "Insert into leagues (discord_server_id, season) Values (@serverID,1)";
            command.Parameters.AddWithValue("@serverID", discordServerID);
            _connection.Open();
            int leagueID = (int)command.ExecuteScalar();
            _connection.Close();
            return leagueID;

        }

        public List<Player> GetPlayerBase(MatchMakingServer server)
        {       
            List<Player> result = new List<Player>();
            MySqlCommand command = _connection.CreateCommand();
            int leagueCount = server.LeagueController.Count;
            var params1 = new string[leagueCount];
            var params2 = new string[leagueCount];
            for (int i = 0; i < leagueCount; i++) {
                params1[i] = string.Format("@season{0}", i);
                params2[i] = string.Format("@leagueid{0}", i);
                command.Parameters.AddWithValue(params1[i], server.LeagueController[i].League.Season);
                command.Parameters.AddWithValue(params2[i], server.LeagueController[i].League.LeagueID);
            }

            command.CommandText = string.Format("Select * from players p " +
                                            "inner join players_leagues pl on p.discord_id = pl.discord_id " +
                                            "Left Outer join player_stats ps on p.discord_id = ps.discord_id " +
                                            "where ps.season IN ({0}) AND pl.league_id IN ({1})", string.Join(", ", params1), string.Join(", ", params2));

           
            _connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                ulong discordId = 0;
                ulong steamId = 0;
                int season = 0;
                int streak = 0;
                int matches = 0;
                int wins = 0;
                int losses = 0;
                int mmr = 0;
                int leagueId = 0;
                for (int i = 0; i < reader.FieldCount; i++) {
                    if (reader.GetName(i).Equals("discord_id"))
                    {
                        discordId = reader.GetUInt64(i);
                    }
                    else if (reader.GetName(i).Equals("steam_id"))
                    {
                        steamId = reader.GetUInt64(i);
                    }
                    else if (reader.GetName(i).Equals("season")) {
                        season = reader.GetInt32(i);
                    }
                    else if (reader.GetName(i).Equals("streak"))
                    {
                        streak = reader.GetInt32(i);
                    }
                    else if (reader.GetName(i).Equals("matches")) {
                        matches = reader.GetInt32(i);
                    }
                    else if (reader.GetName(i).Equals("wins"))
                    {
                        wins = reader.GetInt32(i);
                    }
                    else if (reader.GetName(i).Equals("losses"))
                    {
                        losses = reader.GetInt32(i);
                    }
                    else if (reader.GetName(i).Equals("mmr"))
                    {
                        mmr = reader.GetInt32(i);
                    }
                    else if (reader.GetName(i).Equals("league_id")) {
                        leagueId = reader.GetInt32(i);
                    }
                }
                User discordUser = server.DiscordServer.GetUser(discordId);
                Player player = new Player(discordUser,steamId);
                player.LeagueStats.Add(new LeagueStats(leagueId,season,matches,wins,losses, mmr, streak));
                result.Add(player);
            }
            _connection.Close();

            return result;
        }
    }
}
