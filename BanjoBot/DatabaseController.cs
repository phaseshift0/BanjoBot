using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;

namespace BanjoBot {
    public class DatabaseController {
        private MySqlConnection _connection;
        private IDependencyMap map;

        public DatabaseController(IDependencyMap map)
        {
            string myConnectionString = "server=127.0.0.1;uid=banjo_admin;" + "pwd=D2bblXX!;database=banjoball;";
            _connection = new MySqlConnection();
            _connection.ConnectionString = myConnectionString;
            this.map = map;
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

        public async Task InsertNewPlayer()
        {
            //MySqlCommand command = _connection.CreateCommand();
            //command.CommandText = "Insert into players (discord_server_id, steamID) Values (@serverID,1)";
            //command.Parameters.AddWithValue("@serverID", discordServerID);
            //_connection.Open();
            //int leagueID = (int)command.ExecuteScalar();
            //_connection.Close();
            //return leagueID;
        }

        public  async Task<MatchMakingServer> GetServer(SocketGuild discordServer) {
            //TODO: Get GameCount for current season from sql
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText = "select * from leagues where discord_server_id=@id";
            command.Parameters.AddWithValue("@id",discordServer.Id);
             _connection.Open();
            MySqlDataReader reader = command.ExecuteReader();

            List<League> leagues = new List<League>();
            while (reader.Read())
            {
                int leagueID = 0;
                SocketGuildChannel channel = null;
                SocketRole role = null;
                int season = 0;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.GetName(i).Equals("league_id")) {
                        leagueID = reader.GetInt32(i);
                    }
                    else if (!reader.IsDBNull(i) && reader.GetName(i).Equals("channel_id")) {
                        channel = discordServer.GetChannel(reader.GetUInt64(i));
                    }
                    else if (!reader.IsDBNull(i) && reader.GetName(i).Equals("role_id")) {
                        role = discordServer.GetRole(reader.GetUInt64(i));
                    }
                    else if (reader.GetName(i).Equals("season")) {
                        season = reader.GetInt32(i);
                    }
                }
                leagues.Add(new League(leagueID, season, channel, role));
            }

            _connection.Close();
            if (leagues.Count > 0)
            {
                return new MatchMakingServer(discordServer, leagues);
            }

            return null;

            
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

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.GetName(i).Equals("discord_id"))
                    {
                        discordId = reader.GetUInt64(i);
                    }
                    else if (reader.GetName(i).Equals("steam_id"))
                    {
                        steamId = reader.GetUInt64(i);
                    }
                    else if (reader.GetName(i).Equals("season"))
                    {
                        season = reader.GetInt32(i);
                    }
                    else if (reader.GetName(i).Equals("streak"))
                    {
                        streak = reader.GetInt32(i);
                    }
                    else if (reader.GetName(i).Equals("matches"))
                    {
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
                    else if (reader.GetName(i).Equals("league_id"))
                    {
                        leagueId = reader.GetInt32(i);
                    }
                }
                SocketGuildUser discordUser = server.DiscordServer.GetUser(discordId);
                if (discordUser != null)
                {
                    Player existingPlayer = null;
                    foreach (var p in result)
                    {
                        if (p.User == discordUser)
                        {
                            existingPlayer = p;
                            existingPlayer.LeagueStats.Add(new LeagueStats(leagueId, season, matches, wins, losses, mmr, streak));
                            break;
                        }
                    }
                    if (existingPlayer == null)
                    {
                        Player player = new Player(discordUser, steamId);
                        player.LeagueStats.Add(new LeagueStats(leagueId, season, matches, wins, losses, mmr, streak));
                        result.Add(player);
                    }                
                }
                
        }
            _connection.Close();

            return result;
        }
    }
}
