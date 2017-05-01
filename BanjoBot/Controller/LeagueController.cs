using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace BanjoBot
{
    public class LeagueController
    {

        //TODO: Refactor: Split discord related stuff & controlling
        //TODO: Refactor: Entrypoint for controlling: checkPreconditions() -> Controlling -> SendResultString ->  Controlling
        //^ not its preconditions in start command, some controlling in there "Lobby=null"
        public League League;
        private Lobby Lobby { get; set; }
        public List<Lobby> RunningGames { get; set; }
        private DatabaseController _database;

        public LeagueController(League league)
        {
            RunningGames = new List<Lobby>();
            League = league;
            _database = new DatabaseController();
        }

        private bool LobbyExists()
        {
            return Lobby != null;
        }

        private async Task UpdateChannelWithLobby() {
            SocketTextChannel channel = (SocketTextChannel)League.DiscordInformation.Channel;
            string topic = "";
            if (Lobby == null)
                topic = "Games in progress: " + RunningGames.Count;
            else
                topic = "Open Lobby (" + Lobby.WaitingList.Count + "/8)" + "    Games in progress: " + RunningGames.Count;

            await channel.ModifyAsync(channelProperties => channelProperties.Topic = topic);
        }

        private async Task CancelLobby()
        {
            Lobby = null;
            await UpdateChannelWithLobby();
        }

        private async Task StartGame()
        {
            foreach (Player player in Lobby.WaitingList)
            {
                player.CurrentGame = Lobby;
            }
            Lobby.GameNumber = ++League.GameCounter;
            Lobby.StartGame();
            RunningGames.Add(Lobby);
            await UpdateChannelWithLobby();
            int match_id = await _database.InsertNewMatch(League.LeagueID,League.Season,Lobby.BlueList,Lobby.RedList);
            Lobby.MatchID = match_id;
        }

        private async Task<Lobby> HostGame(Player host)
        {
            Lobby game = new Lobby(host, League);
            Lobby = game;
            return game;
        }

        public async Task RegisterPlayer(Player player)
        {
            League.Applicants.Remove(player);
            player.PlayerStats.Add(new PlayerStats(League.LeagueID, League.Season));

            League.RegisteredPlayers.Add(player);

            if (League.DiscordInformation.LeagueRole != null)
            {
                if (!player.User.RoleIds.Contains(League.DiscordInformation.LeagueRole.Id))
                {
                    await player.User.AddRolesAsync(League.DiscordInformation.LeagueRole);
                }
            }
        }


        private async Task CloseGame(Lobby game, Teams winnerTeam, MatchResult matchData = null)
        {
            RunningGames.Remove(game);
            game.Winner = winnerTeam;
            foreach (Player player in game.WaitingList) {
                player.CurrentGame = null;
            }
            await UpdateChannelWithLobby();
            if (game.StartMessage != null) {
                //TODO: save lobby in db ... lazy fuck
                await game.StartMessage.UnpinAsync();
            }

            if (winnerTeam == Teams.Draw) {
                await _database.DrawMatch(game);
                await ((ITextChannel)League.DiscordInformation.Channel).SendMessageAsync("Closing lobby\nGame " + game.GetGameName() + " has ended in a draw. No stats have been recorded.");
                return;
            }

            game.MmrAdjustment = game.CalculateMmrAdjustment();
            MatchResult matchResult = matchData;
            if (matchData == null)
            {
                matchResult = new MatchResult(game);
            }
            else
            {
                foreach (var stats in matchData.PlayerMatchStats)
                {
                    Player player = game.GetPlayerBySteamID(stats.SteamID);
                    stats.MmrAdjustment = game.MmrAdjustment;
                    if (stats.Team == winnerTeam)
                    {
                        stats.MmrAdjustment = game.MmrAdjustment;
                        stats.StreakBonus = 2* player.GetLeagueStat(League.LeagueID, League.Season).Streak;       
                    }
                    else {
                        stats.MmrAdjustment = -game.MmrAdjustment;
                        stats.StreakBonus = 0;
                    }
                    stats.Win = true;
                    stats.SteamID = player.SteamID;
                    player.Matches.Add(stats);
                }
            }
            await _database.UpdateMatchResult(matchResult);
            League.Matches.Add(matchResult);

            AdjustPlayerStats(game, winnerTeam);
            foreach (var player in game.WaitingList) {
                await _database.UpdatePlayerStats(player, player.GetLeagueStat(game.League.LeagueID, game.League.Season));
            }
            printGameResult(game, winnerTeam, (ITextChannel)League.DiscordInformation.Channel);
        }
        
        public async Task CloseGameByEvent(MatchResult matchResult) {
           
            Lobby lobby = null;
            foreach (var game in RunningGames) {
                if (game.MatchID == matchResult.MatchID) {
                    lobby = game;
                }
            }

            if (lobby == null)
            {
                return;
            }

            matchResult.StatsRecorded = true;
            matchResult.Date = DateTime.Now;
             
            await CloseGame(lobby, matchResult.Winner, matchResult);
        }

        public void AdjustPlayerStats(Lobby game, Teams winner)
        { 
            if (winner == Teams.Blue)
            {
                foreach (var user in game.BlueList)
                {
                    user.IncWins(League.LeagueID, League.Season);
                    user.IncMMR(League.LeagueID, League.Season,
                         game.MmrAdjustment + 2*user.GetLeagueStat(League.LeagueID, League.Season).Streak);
                    user.IncStreak(League.LeagueID, League.Season);
                    user.IncMatches(League.LeagueID, League.Season);
                }
                foreach (var user in game.RedList)
                {

                    user.IncLosses(League.LeagueID, League.Season);
                    user.SetStreakZero(League.LeagueID, League.Season);
                    user.DecMMR(League.LeagueID, League.Season, game.MmrAdjustment);
                    user.IncMatches(League.LeagueID, League.Season);
                    if (user.GetLeagueStat(League.LeagueID, League.Season).MMR < 0)
                        user.SetMMR(League.LeagueID, League.Season, 0);
                }
            }

            if (winner == Teams.Red)
            {
                foreach (var user in game.BlueList)
                {
                    user.IncLosses(League.LeagueID, League.Season);
                    user.SetStreakZero(League.LeagueID, League.Season);
                    user.DecMMR(League.LeagueID, League.Season, game.MmrAdjustment);
                    user.IncMatches(League.LeagueID, League.Season);
                    if (user.GetLeagueStat(League.LeagueID, League.Season).MMR < 0)
                        user.SetMMR(League.LeagueID, League.Season, 0);
                }
                foreach (var user in game.RedList)
                {
                    user.IncWins(League.LeagueID, League.Season);
                    user.IncMMR(League.LeagueID, League.Season,
                         game.MmrAdjustment + 2*user.GetLeagueStat(League.LeagueID, League.Season).Streak);
                    user.IncStreak(League.LeagueID, League.Season);
                    user.IncMatches(League.LeagueID, League.Season);
                }
            }
        }

        public async Task EndGameByModerator(IMessageChannel textChannel, int matchID, Teams team) {
            Lobby startedGame = null;
            foreach (var runningGame in RunningGames) {
                if (runningGame.MatchID == matchID) {
                    startedGame = runningGame;
                    break;
                }
            }

            if (startedGame == null) {
                await textChannel.SendMessageAsync("Match not found");
                return;
            }


            await CloseGame(startedGame, team);

        }

        private async void printGameResult(Lobby game, Teams winnerTeam, ITextChannel textChannel) {
            string message = "Closing lobby\n";

            switch (winnerTeam) {
                case Teams.Red:
                    message += "Red team has won " + game.GetGameName() + "!\n";
                    message += GetGameResultString(game, Teams.Red);
                    break;
                case Teams.Blue:
                    message += "Blue team has won " + game.GetGameName() + "!\n";
                    message += GetGameResultString(game, Teams.Blue);

                    break;
                case Teams.Draw:
                    message += "Game " + game.GetGameName() + " has ended in a draw. No stats have been recorded.";
                    break;
            }

            await textChannel.SendMessageAsync(message);

        }

        private String GetGameResultString(Lobby game, Teams winner) {
            char blueSign = '+';
            char redSign = '+';
            if (Teams.Blue == winner)
                redSign = '-';
            else
                blueSign = '-';

            String message = "";
            message += "Blue team (" + blueSign + game.MmrAdjustment + "): ";
            foreach (var player in game.BlueList) {
                if (player.GetLeagueStat(League.LeagueID, League.Season).Streak > 1)
                    message += player.PlayerMMRString(League.LeagueID, League.Season) + "+" + 2 * (player.GetLeagueStat(League.LeagueID, League.Season).Streak - 1) + " ";
                else
                    message += player.PlayerMMRString(League.LeagueID, League.Season) + " ";
            }
            message += "\n";
            message += "Red team (" + redSign + game.MmrAdjustment + "): ";
            foreach (var player in game.RedList) {
                if (player.GetLeagueStat(League.LeagueID, League.Season).Streak > 1)
                    message += player.PlayerMMRString(League.LeagueID, League.Season) + "+" + 2 * (player.GetLeagueStat(League.LeagueID, League.Season).Streak - 1) + " ";
                else
                    message += player.PlayerMMRString(League.LeagueID, League.Season) + " ";
            }

            return message;
        }

        /// <summary>
        /// Creates a new Game, binding the host and broadcasting to the Channel.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="host">The User who hosted the game.</param>
        public async Task TryHostGame(IMessageChannel textChannel, Player host)
        {
            if (host.IsIngame())
            {
                await WriteMessage(textChannel, host.User.Mention + " Vote before hosting another game");
                return;
            }

            // If no games are open.
            if (!LobbyExists())
            {
                Lobby newGame = await HostGame(host);
                await UpdateChannelWithLobby();
                await textChannel.SendMessageAsync("New Lobby created by " + host.PlayerMMRString(League.LeagueID, League.Season) + ". \nType !join to join the game. (" + newGame.WaitingList.Count() + "/8)");
            } else { 
                await WriteMessage(textChannel, host.User.Mention + " Lobby is already open. Only one Lobby may be hosted at a time. \nType !join to join the game.");
            }
        }

        /// <summary>
        /// Adds a User to the currently active Game (if one exists) and broadcasts to the SocketGuildChannel.
        /// </summary>
        /// <param name="textChannel">The SocketGuildChannel to be broadcasted to.</param>
        /// <param name="player">User who wishes to join.</param>
        public async Task JoinGame(IMessageChannel textChannel, Player player)
        {
            if (player.IsIngame())
            {
                await WriteMessage(textChannel, player.User.Mention + " Vote before joining another game");
                return;
            } 

            if (!LobbyExists())
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to add player
            bool? addPlayerResult = Lobby.AddPlayer(player);

            // If successful
            if (addPlayerResult == true)
            {
                await UpdateChannelWithLobby();
                await textChannel.SendMessageAsync(player.PlayerMMRString(League.LeagueID, League.Season) + " has joined the lobby. (" + Lobby.WaitingList.Count() + "/8)");
                if (Lobby.WaitingList.Count() == 8) { 
                    await textChannel.SendMessageAsync(Lobby.Host.User.Mention + ", The lobby is full. Type !startgame to start the game");
                    await (await (Lobby.Host.User as IGuildUser).CreateDMChannelAsync()).SendMessageAsync("The lobby is full. Type !startgame to start the game");
                }
            }

            // If unsuccessfull
            else if (addPlayerResult == false)
                await WriteMessage(textChannel, player.User.Mention + " The Lobby is full.");
            // If player already in game
            else if (addPlayerResult == null)
                await WriteMessage(textChannel, player.User.Mention + " you can not join a game you are already in.");
            else
                await WriteMessage(textChannel, "Error: Command.joinGame()");
        }

        /// <summary>
        /// Removes a User from the currently active Game (if one exists) and broadcasts to the Channel.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="user">User who wishes to leave.</param>
        public async Task LeaveGame(IMessageChannel textChannel, Player user)
        {
            // If no games are open.
            if (!LobbyExists())
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to remove player
            bool? removePlayerResult = Lobby.RemovePlayer(user);

            // If successful
            if (removePlayerResult == true)
            {
                await textChannel.SendMessageAsync(user.PlayerMMRString(League.LeagueID, League.Season) +" has left the lobby. (" + Lobby.WaitingList.Count() + "/8)");
                await UpdateChannelWithLobby();
            }
            // If game now empty
            else if (removePlayerResult == false)
            {
                await
                    textChannel.SendMessageAsync(user.PlayerMMRString(League.LeagueID, League.Season) +
                                                 " has left the lobby. (" + Lobby.WaitingList.Count() + "/8)");
                await textChannel.SendMessageAsync("Closing Lobby");
                await CancelLobby();
            }
            // If player not in game
            else if (removePlayerResult == null)
                await WriteMessage(textChannel, user.User.Mention + " you are not in this game.");
            else
                await WriteMessage(textChannel, "Error: Command.leaveGame()");
        }

        /// <summary>
        /// Kicks a player
        /// </summary>
        public async Task KickPlayer(IMessageChannel textChannel, Player user) {
            // If no games are open.
            if (!LobbyExists()) {
                await WriteMessage(textChannel, "No games open.");
                return;
            }

            // Attempt to remove player
            bool? removePlayerResult = Lobby.RemovePlayer(user);

            // If successful
            if (removePlayerResult == true) {
                await UpdateChannelWithLobby();
                await textChannel.SendMessageAsync(user.PlayerMMRString(League.LeagueID, League.Season) + " got kicked from the lobby. (" + Lobby.WaitingList.Count() + "/8)");
            }
            // If game now empty
            else if (removePlayerResult == false) {
                await textChannel.SendMessageAsync(user.PlayerMMRString(League.LeagueID, League.Season) + " got kicked got kicked from the lobby. (" + Lobby.WaitingList.Count() + "/8)");
                await textChannel.SendMessageAsync("Closing lobby");
                await CancelLobby();
            }
            // If player not in game
            else if (removePlayerResult == null)
                await WriteMessage(textChannel, user.User.Mention + " is not in the game.");
            else
                await WriteMessage(textChannel, "Error: Command.leaveGame()");
        }

        /// <summary>
        /// Starts the currently active game. Only the host can use this command sucessfully.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="playerUser who wishes to leave.</param>
        public async Task StartGame(IMessageChannel textChannel, Player player)
        {
            if (!LobbyExists())
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // If the player who started the game was not the host
            if (Lobby.Host != player) { 
                await WriteMessage(textChannel, player.User.Mention + " only the host (" + Lobby.Host.User.Username + ") can start the game.");
                return;
            }else if (Lobby.WaitingList.Count < 1) { 
                await WriteMessage(textChannel, player.User.Mention + " you need 8 players to start the game.");
                return;
            }

            // If the game sucessfully started
            await StartGame();
            String startmessage = Lobby.GetGameName() + "("+ Lobby.MatchID +")" + " has been started by " + player.PlayerMMRString(League.LeagueID,League.Season) + ".";
            // Prepare Blue Team
            String blueTeam = "Blue Team (" + Lobby.GetTeamMMR(Teams.Blue) + "): ";
            foreach (var p in Lobby.BlueList)
            {
                blueTeam += p.User.Mention + "(" + p.GetLeagueStat(League.LeagueID, League.Season).MMR + ") ";   
            }

            // Prepare Red Team
            String redTeam = "Red Team (" + Lobby.GetTeamMMR(Teams.Red) + "): ";
            foreach (var p in Lobby.RedList)
            {
                redTeam += p.User.Mention + "(" + p.GetLeagueStat(League.LeagueID, League.Season).MMR + ") ";
            }

            // Broadcast teams and password
            Lobby.StartMessage = await textChannel.SendMessageAsync(startmessage + "\n" + blueTeam + "\n" + redTeam + "\nPassword: " + Lobby.GeneratePassword(6));
            await Lobby.StartMessage.PinAsync();

            foreach (var p in Lobby.WaitingList)
            {
                await (await (p.User as IGuildUser).CreateDMChannelAsync()).SendMessageAsync(Lobby.GetGameName() +  " has been started");
            }

            Lobby = null;
            await UpdateChannelWithLobby();
        }

        /// <summary>
        /// Cancels the currently active game. Only the host can use this.
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to.</param>
        /// <param name="player">User who called the function.</param>
        public async Task CancelLobby(IMessageChannel textChannel, Player player)
        {
            if (!LobbyExists())
            {
                await textChannel.SendMessageAsync("No games open. Type !hostgame to create a game.");
                return;
            }
             
            //TODO: refactor
            if (player == Lobby.Host || player.User.RoleIds.Contains(League.DiscordInformation.ModeratorRole.Id) || player.User.GuildPermissions.Administrator)
            {
                await textChannel.SendMessageAsync("Game canceled by host " + player.User.Username + ".");     
                await CancelLobby();
            } 
        }

        /// <summary>
        /// Records the vote and anmnounces a winner if one is determined.
        /// </summary>
        /// <param name="player">User who voted.</param>
        /// <param name="team">Team that the player voted for.</param>
        public async Task VoteCancel(IMessageChannel textChannel, Player player)
        {
            if (Lobby == null)
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            if (!Lobby.WaitingList.Contains(player)) { 
                await WriteMessage(textChannel, player.User.Mention + " only players who were in the game can vote.");
                return;
            }
            if (Lobby.CancelCalls.Contains(player)) { 
                await WriteMessage(textChannel, player.User.Mention + " you have already voted.");
                return;
            }

            Lobby.CancelCalls.Add(player);
            if (Lobby.CancelCalls.Count >= Math.Ceiling((double) Lobby.WaitingList.Count()/2))
            {
                await textChannel.SendMessageAsync("Lobby got canceled canceled by vote.");
                await CancelLobby();
            }
            else
            {
                await textChannel.SendMessageAsync("Vote recorded to cancel game by " + player.User.Username + " (" + Lobby.CancelCalls.Count() + "/" + Math.Ceiling((double)Lobby.WaitingList.Count() / 2) + ")");
            }   
        }

        /// <summary>
        /// Lists all of the players in the currently ActiveGame
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to.</param>
        public async Task ListPlayers(IMessageChannel textChannel)
        {
            if (Lobby == null)
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            String message = "Lobby (" + Lobby.WaitingList.Count() + "/8)  player: \n";
            foreach (var user in Lobby.WaitingList)
            {
                message += user.PlayerMMRString(League.LeagueID,League.Season) + " ";
            }
            await WriteMessage(textChannel, message);
        }

        /// <summary>
        /// Records the vote and anmnounces a winner if one is determined.
        /// </summary>
        /// <param name="e">MessageEventArgs, used to extract gameName and Channel</param>
        /// <param name="player">User who voted.</param>
        /// <param name="team">Team that the player voted for.</param>
        public async Task VoteWinner(IMessageChannel textChannel, Player player, Teams team)
        {
            if (!player.IsIngame())
            {
                await WriteMessage(textChannel, player.User.Mention + " you are not in a game.");
                return;
            }

            Lobby game = player.CurrentGame;
            if (game.BlueWinCalls.Contains(player))
            {
                if (team == Teams.Blue || team == Teams.Draw)
                {
                    await WriteMessage(textChannel, player.User.Mention + " you have already voted for this team.");
                }
                else if (team == Teams.Red)
                {
                    game.BlueWinCalls.Remove(player);
                    game.RedWinCalls.Add(player);
                    await WriteMessage(textChannel, player.User.Mention + " has changed his Mind");
                    await textChannel.SendMessageAsync("Vote recorded for Red team in game " + game.GetGameName() + " by " + player.User.Username + ". (" + game.RedWinCalls.Count() + "/5)");
                }
            }
            else if (game.RedWinCalls.Contains(player))
            {
                if (team == Teams.Red || team == Teams.Draw) {
                    await WriteMessage(textChannel, player.User.Mention + " you have already voted for this team.");
                }
                else if (team == Teams.Blue) {
                    game.RedWinCalls.Remove(player);
                    game.BlueWinCalls.Add(player);
                    await WriteMessage(textChannel, player.User.Mention + " has changed his Mind");
                    await textChannel.SendMessageAsync("Vote recorded for Blue team in game " + game.GetGameName() + " by " + player.User.Username + ". (" + game.BlueWinCalls.Count() + "/5)");
                }
            }
            else if (game.DrawCalls.Contains(player))
            {
                await WriteMessage(textChannel, player.User.Mention + " you have already voted");
            }
            else
            {
                switch (team)
                {
                    case Teams.Red:
                        game.RedWinCalls.Add(player);
                        await textChannel.SendMessageAsync("Vote recorded for Red team in game " + game.GetGameName() + " by " + player.User.Username + ". (" + game.RedWinCalls.Count() + "/5)");
                        break;
                    case Teams.Blue:
                        game.BlueWinCalls.Add(player);
                        await textChannel.SendMessageAsync("Vote recorded for Blue team in game " + game.GetGameName() + " by " + player.User.Username + ". (" + game.BlueWinCalls.Count() + "/5)");
                        break;
                    case Teams.Draw:
                        game.DrawCalls.Add(player);
                        await textChannel.SendMessageAsync("Vote recorded for draw " + game.GetGameName() + " by " + player.User.Username + ". (" + game.DrawCalls.Count() + "/5)");
                        break;
                }
            }

            if (game.BlueWinCalls.Count == Lobby.VOTETHRESHOLD)
            {
                await CloseGame(game, Teams.Blue);
            }
            if (game.RedWinCalls.Count == Lobby.VOTETHRESHOLD) {
                await CloseGame(game, Teams.Red);
            }
            if (game.DrawCalls.Count == Lobby.VOTETHRESHOLD) {
                await CloseGame(game, Teams.Draw);
            }
        }

        /// <summary>
        /// Prints a list of all the games currently open/in progress.
        /// </summary>
        /// <param name="textChannel">Channel to send the message to.</param>
        public async Task ShowGames(IMessageChannel textChannel)
        {
            if (Lobby != null)
                await WriteMessage(textChannel, "Open lobby: (" + Lobby.WaitingList.Count() + "/8)");
            else
                await WriteMessage(textChannel, "No games in lobby.");

            if (RunningGames.Count > 0)
            {
                String message = "Games in progress: ";
                foreach (var game in RunningGames)
                {
                    message += game.GetGameName() + "("+ game.MatchID +"), ";
                }
                await WriteMessage(textChannel, message);
            }
            else
                await WriteMessage(textChannel, "No games in progress.");
        }

        /// <summary>
        /// Prints a users stats to the text channel.
        /// </summary>
        /// <param name="textChannel">Channel to send message to.</param>
        /// <param name="user">Users whos stats will be displayed.</param>
        public async Task GetStats(IMessageChannel textChannel, Player player)
        {
            if (player != null)
            {
                int wins = player.GetLeagueStat(League.LeagueID, League.Season).Wins;
                int losses = player.GetLeagueStat(League.LeagueID, League.Season).Losses;
                int gamesPlayed = wins + losses;
                await WriteMessage(textChannel, player.PlayerMMRString(League.LeagueID,League.Season) + " has " + gamesPlayed + " games played, " + wins + " wins, " + losses + " losses.\nCurrent win streak: " + player.GetLeagueStat(League.LeagueID, League.Season).Streak + ".");
            }
            else
                await WriteMessage(textChannel, player.User.Username + " has no recorded stats.");
        }

        public async Task ShowPlayerProfile(Player  player, int season)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(new Color(0, 0, 255));
            builder.Title = player.User.Username + "'s profile";
            float goals = 0;
            float assist = 0;
            float steals = 0;
            float turnovers = 0;
            float st = 0;
            float pickups = 0;
            float passes = 0;
            float pr = 0;
            float save = 0;
            float points = 0;
            float post = 0;
            float tag = 0;
            int statsRecorded = 0;

            List<PlayerMatchStats> seasonMatchStats = player.GetMatchesBySeason(League.LeagueID, season);
            if (seasonMatchStats.Count == 0)
            {
                await (await (player.User as IGuildUser).CreateDMChannelAsync()).SendMessageAsync("No stats found");
                return;
            }
            
            foreach (var matchStats in seasonMatchStats) {
                if (matchStats.Match.StatsRecorded) {
                    statsRecorded++;
                    goals += matchStats.Goals;
                    assist += matchStats.Assist;
                    steals += matchStats.Steals;
                    turnovers += matchStats.Turnovers;
                    st += matchStats.StealTurnDif;
                    pickups += matchStats.Pickups;
                    passes += matchStats.Passes;
                    pr += matchStats.PassesReceived;
                    save += matchStats.SaveRate;
                    points += matchStats.Points;
                    post += matchStats.PossessionTime;
                    tag += matchStats.TimeAsGoalie;
                }
            }
            PlayerStats playerStats = player.GetLeagueStat(League.LeagueID, season);
            string message = $"**{player.User.Username}'s Profile**\n`";
            message += $"{"League",-24} {League.Name,-12} \n";
            message += $"{"Season",-24} {season,-12} \n";
            message += $"{"Matches",-24} {playerStats.MatchCount,-12} \n";
            message += $"{"Wins",-24} {playerStats.Wins,-12} \n";
            message += $"{"Losses",-24} {playerStats.Losses,-12} \n";
            message += $"{"Winrate",-24} {playerStats.MatchCount/playerStats.Wins,-12:P} \n";
            message += $"{"Streak",-24} {playerStats.Streak,-12} \n";
            message += $"{"Rating",-24} {playerStats.MMR,-12} `\n";
            message += "\n";
            message += $"**AverageStats**\n`";
            message += $"{"Goals",-24} {statsRecorded/goals,-12:N} \n";
            message += $"{"Assist",-24} {statsRecorded/assist,-12:N} \n";
            message += $"{"Steals",-24} {statsRecorded/steals,-12:N} \n";
            message += $"{"Turnovers",-24} {statsRecorded/turnovers,-12:N} \n";
            message += $"{"S-T",-24} {statsRecorded/st,-12:N} \n";
            message += $"{"Pickups",-24} {statsRecorded/pickups,-12:N} \n";
            message += $"{"Passes",-24} {statsRecorded/passes,-12:N} \n";
            message += $"{"PR",-24} {statsRecorded/pr,-12:N} \n";
            message += $"{"SaveRate",-24} {statsRecorded/save,-12:P} \n";
            message += $"{"Points",-24} {statsRecorded/points,-12:N} \n";
            message += $"{"PosT",-24} {statsRecorded/post,-12:N} \n";
            message += $"{"TAG",-24} {statsRecorded/tag,-12:N} \n`";
            message += $"\n*Stats for {statsRecorded} of {playerStats.MatchCount} games were recorded*";
            builder.Description = message;
            await (await (player.User as IGuildUser).CreateDMChannelAsync()).SendMessageAsync(message);
        }

        public async Task GetMatchHistory(Player player, int season)
        {
            object[] args = new object[] {"Date", "MatchID", "Goals", "Assist", "Steals", "Turnovers", "S/T", "Pickups", "Passes", "PR", "Save", "Points", "PosT", "TAG", "Mmr", "Streak","Stats","Hero" };
            String s = String.Format("{0,-12} {1,-8} {17,-10} {2,-8} {3,-8} {4,-8} {5,-10} {6,-8} {7,-8} {8,-8} {9,-8} {10,-8} {11,-8} {12,-8} {13,-8} {14,-8} {15,-8} {16,-8}\n", args);
            List<PlayerMatchStats> allStats = player.GetMatchesBySeason(League.LeagueID, season);
            IOrderedEnumerable<PlayerMatchStats> orderedStats = allStats.OrderByDescending(stats => stats.Match.Date);
            for(int i = 0; i < 10; i++)
            {
                PlayerMatchStats stats = orderedStats.ElementAt(i);
                args = new object[] { DateTime.Parse(stats.Match.Date.ToString()).ToShortDateString(),stats.Match.MatchID,stats.Goals, stats.Assist, stats.Steals, stats.Turnovers, stats.StealTurnDif, stats.Pickups, stats.Passes, stats.PassesReceived, stats.SaveRate, stats.Points,stats.PossessionTime,stats.TimeAsGoalie,stats.MmrAdjustment,stats.StreakBonus,stats.Match.StatsRecorded,"Storm"};
                s += String.Format("{0,-12} {1,-8} {17,-10} {2,-8} {3,-8} {4,-8} {5,-10} {6,-8} {7,-8} {8,-8} {9,-8:P0} {10,-8} {11,-8} {12,-8} {13,-8} {14,-8} {15,-8} {16,-8}\n", args);
            }
            if (!orderedStats.Any())
            {
                await (await (player.User as IGuildUser).CreateDMChannelAsync()).SendMessageAsync("You have not played in season " + season);
            }
            else
            {
                await (await (player.User as IGuildUser).CreateDMChannelAsync()).SendMessageAsync("```" + s + "```");
            }
            

        }

        public async Task GetMatchHistory(Player player)
        {
            await GetMatchHistory(player, League.Season);

        }

        public async Task ShowPlayerProfile(Player player)
        {
            await ShowPlayerProfile(player, League.Season);
        }

        /// <summary>
        /// Broadcasts the top 5 players and their MMR to the text channel.
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to</param>
        /// <param name="ds">Database object</param>
        public async Task ShowTopMMR(IMessageChannel textChannel)
        {
            // Sort dictionary by MMR
            var sortedDict = from entry in League.RegisteredPlayers orderby entry.GetLeagueStat(League.LeagueID, League.Season).MMR descending select entry ;

            await WriteMessage(textChannel, "Top 5 players by MMR:");
            string message = "";
            int i = 0;
            foreach (var obj in sortedDict)
            {
                if (i == 5)
                    break;
                message += "#" + (i + 1) + " " + obj.PlayerMMRString(League.LeagueID,League.Season) + ", ";
                i++;
            }
            await WriteMessage(textChannel, message);
        }

        public async Task ReCreateLobby(IMessageChannel textChannel, int matchID, IGuildUser playerToRemove)
        {
            Lobby startedGame = null;
            foreach (var runningGame in RunningGames) {
                if (runningGame.MatchID == matchID)
                {
                    startedGame = runningGame;
                    break;
                }
            }

            if (startedGame == null)
            {
                await textChannel.SendMessageAsync("Match not found");
                return;
            }

            await CloseGame(startedGame, Teams.Draw);

            if (!LobbyExists())
            {
                await HostGame(startedGame.Host);
            }

            if (Lobby.WaitingList.Count == 1)
            {
                foreach (var player in startedGame.WaitingList)
                {
                    if (player.User.Id != playerToRemove.Id && !Lobby.WaitingList.Contains(player))
                        Lobby.AddPlayer(player);
                }
                string message = "";
                if (League.DiscordInformation.LeagueRole != null)
                    message = League.DiscordInformation.LeagueRole.Mention;
                await textChannel.SendMessageAsync(message + "Lobby recreated  (" + Lobby.WaitingList.Count() + "/8)");
            }
            else
            {
                await textChannel.SendMessageAsync("There is already a open Lobby with more than 1 player, please rejoin yourself");
            }

            await UpdateChannelWithLobby();

        }



        public async Task StartNewSeason(IMessageChannel textChannel)
        {
            String message = "";
            message = "**Season " + League.Season + " has ended.**\n";
            message += "**Top Players Season " + League.Season + ": **\n";
            var sortedDict = from entry in League.RegisteredPlayers orderby entry.GetLeagueStat(League.LeagueID, League.Season).MMR descending select entry;

            object[] args = new object[] { "Name", "MMR", "Matches", "Wins", "Losses" };
            message += String.Format("{0,-10} {1,-10} {2,-10} {3,-10} {4,-10}\n", args);
            for (int i = 0; i < sortedDict.Count(); i++) {
                if (i < 10) {
                    PlayerStats stats = sortedDict.ElementAt(i).GetLeagueStat(League.LeagueID, League.Season);
                    string username = sortedDict.ElementAt(i).User.Username.Length > 8
                        ? sortedDict.ElementAt(i).User.Username.Substring(0, 8)
                        : sortedDict.ElementAt(i).User.Username;
                    args = new object[] { username, stats.MMR, stats.MatchCount, stats.Wins, stats.Losses };
                    message += String.Format("{0,-10} {1,-10} {2,-10} {3,-10} {4,-10}\n", args);
                }

                PlayerStats newStats = new PlayerStats(League.LeagueID, League.Season + 1);
                sortedDict.ElementAt(i).PlayerStats.Add(newStats);
                await _database.UpdatePlayerStats(sortedDict.ElementAt(i), newStats);
            }

            await textChannel.SendMessageAsync("```" + message + "```");
            League.Season++;
            League.Matches = new List<MatchResult>();
            League.GameCounter = 0;
            await _database.UpdateLeague(League);
        
        }
        
        /// <summary>
        /// Used to send temporary messages. Sends a message to the Channel then deletes the message after a delay.
        /// </summary>
        /// <param name="textChannel">Channel to send the message to</param>
        /// <param name="message">Message to be sent to the channel</param>
        /// <returns></returns>
        private async Task WriteMessage(IMessageChannel textChannel, String message)
        {
            // Send message
            IUserMessage discordMessage = await textChannel.SendMessageAsync(message);
            
            // Delete message
            System.Threading.ThreadPool.QueueUserWorkItem(delegate {
                deleteMessage(discordMessage);
            }, null);
        }

        /// <summary>
        /// Deletes the message after a delay.
        /// </summary>
        /// <param name="message"></param>
        private void deleteMessage(IUserMessage message)
        {
            System.Threading.Thread.Sleep(20 * 1000);
            message.DeleteAsync();
        }

    }
}
