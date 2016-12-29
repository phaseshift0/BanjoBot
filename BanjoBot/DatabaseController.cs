using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;

namespace BanjoBot
{
    public class DatabaseController
    {
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

        public async Task UpdateLeague(League league)
        {
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText =
                "Update leagues Set channel_id=@channel_id,role_id=@role_id,season=@season,name=@name,auto_accept=@autoaccept,need_steam_register=@steamreg, moderator_role=@modrole " +
                "Where league_id = @league_id";
            command.Parameters.AddWithValue("@league_id", league.LeagueID);
            command.Parameters.AddWithValue("@season", league.Season);
            command.Parameters.AddWithValue("@name", league.Name);
            command.Parameters.AddWithValue("@autoaccept", league.AutoAccept);
            command.Parameters.AddWithValue("@steamreg", league.NeedSteamToRegister);
            if (league.ModeratorRole == null)
                command.Parameters.AddWithValue("@modrole", DBNull.Value);
            else
                command.Parameters.AddWithValue("@modrole", league.ModeratorRole.Id);
            if (league.Role == null)
                command.Parameters.AddWithValue("@role_id", DBNull.Value);
            else
                command.Parameters.AddWithValue("@role_id", league.Role.Id);
            if (league.Channel == null)
                command.Parameters.AddWithValue("@channel_id", DBNull.Value);
            else
                command.Parameters.AddWithValue("@channel_id", league.Channel.Id);
            try
            {

                _connection.Open();
                command.ExecuteNonQuery();
                _connection.Close();
            }
            catch (Exception e)
            {
                _connection.Close();
                Console.WriteLine(e.Message);
            }

        }


        public async Task UpdatePlayerStats(Player player, LeagueStats playerstats)
        {
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText =
                "REPLACE INTO player_stats (league_id,season,discord_id,matches,win,losses,mmr,streak) Values (@league_id,@season,@discord_id,@matches,@win,@losses, @mmr,@streak)";
            command.Parameters.AddWithValue("@matches", playerstats.Matches);
            command.Parameters.AddWithValue("@win", playerstats.Wins);
            command.Parameters.AddWithValue("@losses", playerstats.Losses);
            command.Parameters.AddWithValue("@mmr", playerstats.MMR);
            command.Parameters.AddWithValue("@streak", playerstats.Streak);
            command.Parameters.AddWithValue("@league_id", playerstats.LeagueID);
            command.Parameters.AddWithValue("@discord_id", player.User.Id);
            command.Parameters.AddWithValue("@season", playerstats.Season);

            try
            {

                _connection.Open();
                command.ExecuteNonQuery();
                _connection.Close();
            }
            catch (Exception e)
            {
                _connection.Close();
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

        }

        public async Task InsertNewPlayer(Player player)
        {
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText = "Insert into players (discord_id, steam_id) Values (@discordID,@steamID)";
            command.Parameters.AddWithValue("@steamID", player.SteamID);
            command.Parameters.AddWithValue("@discordID", player.User.Id);
            _connection.Open();
            command.ExecuteNonQuery();
            _connection.Close();
        }

        public async Task RegisterPlayerToLeague(Player player, League league)
        {
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText =
                "Insert into players_leagues (discord_id, league_id, approved) Values (@discordID,@league_id,1) ON DUPLICATE KEY UPDATE approved=1";
            command.Parameters.AddWithValue("@discordID", player.User.Id);
            command.Parameters.AddWithValue("@league_id", league.LeagueID);
            _connection.Open();
            command.ExecuteNonQuery();
            _connection.Close();
        }


        public async Task InsertSignupToLeague(ulong discordID, League league)
        {
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText =
                "Insert into players_leagues (discord_id, league_id, approved) Values (@discordID,@league_id,0)";
            command.Parameters.AddWithValue("@discordID", discordID);
            command.Parameters.AddWithValue("@league_id", league.LeagueID);
            _connection.Open();
            command.ExecuteNonQuery();
            _connection.Close();
        }


        //TODO: change updateLeague to insert or update on duplicate
        public async Task<int> InsertNewLeague(ulong discordServerID)
        {
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText = "Insert into leagues (discord_server_id, season) Values (@serverID,1);SELECT LAST_INSERT_ID();";
            command.Parameters.AddWithValue("@serverID", discordServerID);
            try
            {
                _connection.Open();
                int leagueID = Convert.ToInt32(command.ExecuteScalar());
                _connection.Close();
                return leagueID;
            }
            catch (Exception e)
            {
                _connection.Close();
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
                return -1;
            }
         
           

        }

        public async Task<MatchMakingServer> GetServer(SocketGuild discordServer)
        {
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText = "select *," +
                                  "(select count(*) from matches m where l.league_id = m.league_id AND l.season = m.season) as match_count" +
                                  " from leagues l where discord_server_id=@id";
            command.Parameters.AddWithValue("@id", discordServer.Id);
            _connection.Open();
            try
            {
                MySqlDataReader reader = command.ExecuteReader();
                List<League> leagues = new List<League>();
                while (reader.Read())
                {
                    int leagueID = 0;
                    SocketGuildChannel channel = null;
                    SocketRole role = null;
                    int season = 0;
                    string name = "";
                    bool autoAccept = false;
                    bool needSteamReg = false;
                    SocketRole modRoleID = null;
                    int match_count = 0;

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
                        else if (!reader.IsDBNull(i) && reader.GetName(i).Equals("name")) {
                            name = reader.GetString(i);
                        }
                        else if (!reader.IsDBNull(i) && reader.GetName(i).Equals("auto_accept")) {
                            autoAccept = reader.GetBoolean(i);
                        }
                        else if (!reader.IsDBNull(i) && reader.GetName(i).Equals("need_steam_register"))
                        {
                            needSteamReg = reader.GetBoolean(i);
                        }
                        else if (!reader.IsDBNull(i) && reader.GetName(i).Equals("moderator_role")) {
                            modRoleID = discordServer.GetRole(reader.GetUInt64(i));
                        }
                        else if (!reader.IsDBNull(i) && reader.GetName(i).Equals("match_count")) {
                            match_count = reader.GetInt32(i);
                        }

                    }
                    League league = new League(leagueID, season, channel, modRoleID, match_count);
                    league.Role = role;
                    league.Name = name;
                    league.AutoAccept = autoAccept;
                    league.NeedSteamToRegister = needSteamReg;
                    leagues.Add(league);
                }

                _connection.Close();
                if (leagues.Count > 0)
                {
                    return new MatchMakingServer(discordServer, leagues);
                }

                return null;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                throw;
            }


        }

        //TODO: Get approvals!
        public async Task<List<Player>> GetPlayerBase(MatchMakingServer server)
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
                                            "where ps.season IN ({0}) " +
                                             "AND pl.league_id IN ({1}) " +
                                             "AND pl.approved = 1", string.Join(", ", params1), string.Join(", ", params2));

           
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

        public async Task DeleteLeague(League league)
        {
            MySqlCommand command = _connection.CreateCommand();
            command.CommandText = "Delete from leagues where league_id = @league_id";
            command.Parameters.AddWithValue("@league_id", league.LeagueID);
            _connection.Open();
            command.ExecuteNonQuery();
            _connection.Close();
        }
    }
}
