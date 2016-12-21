using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace BanjoBot
{
    public class LeagueController
    {
        //TODO: ModeratorRole to league and AdminRole to MyServer
        public League League;
        private MatchMakingServer _server;
        private Game ActiveGame { get; set; }
        private List<Game> RunningGames { get; set; }

        public LeagueController(MatchMakingServer server, League league)
        {
            RunningGames = new List<Game>();
            _server = server;
            League = league;
        }

        public bool LobbyExists() {
            return ActiveGame != null;
        }

        public void CancelGame() {
            ActiveGame = null;
        }

        public void StartGame() {
            foreach (Player player in ActiveGame.WaitingList) {
                player.CurrentGame = ActiveGame;
            }
            ActiveGame.StartGame();
            RunningGames.Add(ActiveGame);
            ActiveGame = null;
        }

        public void CloseGame(Game game, Teams winnerTeam) {
            game.AdjustStats(winnerTeam);
            RunningGames.Remove(game);
            foreach (Player player in game.WaitingList) {
                player.CurrentGame = null;
            }

            //saveData(); //TODO:
        }

        public async Task<Game> HostGame(Player host) {
            //TODO: only increment gamecounter on finish
            Game game = new Game(host, League.GameCounter + 1,League.LeagueID);
            ActiveGame = game;
            return game;
        }



        public async Task RegisterPlayer(Player player)
        {
            player.LeagueStats.Add(new LeagueStats(League.LeagueID,League.Season));
            League.RegisteredPlayers.Add(player);
            _server.RegisteredPlayers.Add(player);
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
                
                Game newGame = await HostGame(host);
                await textChannel.SendMessageAsync("New game " + newGame.GameName + " hosted by " + host.PlayerMMRString(League.LeagueID) + ". \nType !join to join the game. (" + newGame.WaitingList.Count() + "/8)");
            } else { 
                await WriteMessage(textChannel, host.User.Mention + " Game " + ActiveGame.GameName + " is already open. Only one game may be hosted at a time. \nType !join to join the game.");
            }
        }

        /// <summary>
        /// Adds a User to the currently active Game (if one exists) and broadcasts to the SocketGuildChannel.
        /// </summary>
        /// <param name="textChannel">The SocketGuildChannel to be broadcasted to.</param>
        /// <param name="player">User who wishes to join.</param>
        public async Task JoinGame(IMessageChannel textChannel, Player player)
        {
            Console.WriteLine("joinGame()");
            if (player.IsIngame())
            {
                await WriteMessage(textChannel, player.User.Mention + " Vote before joining another game");
                return;
            } 
            if (LobbyExists())
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to add player
            bool? addPlayerResult = ActiveGame.AddPlayer(player);

            // If successful
            if (addPlayerResult == true)
            {
                await textChannel.SendMessageAsync(player.PlayerMMRString(League.LeagueID) + " has joined " + ActiveGame.GameName + ". (" + ActiveGame.WaitingList.Count() + "/8)");
                if (ActiveGame.WaitingList.Count() == 8) { 
                    await textChannel.SendMessageAsync(ActiveGame.Host.User.Mention + ", The lobby is full. Type !startgame to start the game");
                }
            }

            // If unsuccessfull
            else if (addPlayerResult == false)
                await WriteMessage(textChannel, player.User.Mention + " The game " + ActiveGame.GameName + " is full.");
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
        public async Task leaveGame(IMessageChannel textChannel, Player user)
        {
            // If no games are open.
            if (LobbyExists())
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to remove player
            bool? removePlayerResult = ActiveGame.RemovePlayer(user);

            // If successful
            if (removePlayerResult == true)
                await textChannel.SendMessageAsync(user.PlayerMMRString(League.LeagueID) + " has left " + ActiveGame.GameName + ". (" + ActiveGame.WaitingList.Count() + "/8)");
            // If game now empty
            else if (removePlayerResult == false)
            {
                await WriteMessage(textChannel, user.PlayerMMRString(League.LeagueID) + " has left " + ActiveGame.GameName + ". (" + ActiveGame.WaitingList.Count() + "/8)");
                await WriteMessage(textChannel,"Closing game " + ActiveGame.GameName);
                CancelGame();
            }
            // If player not in game
            else if (removePlayerResult == null)
                await WriteMessage(textChannel, user.User.Mention + " you are not in this game.");
            else
                await WriteMessage(textChannel, "Error: Command.leaveGame()");
        }

        /// <summary>
        /// Starts the currently active game. Only the host can use this command sucessfully.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="playerUser who wishes to leave.</param>
        public async Task startGame(IMessageChannel textChannel, Player player)
        {
            if (!LobbyExists())
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // If the player who started the game was not the host
            if (ActiveGame.Host != player) { 
                await WriteMessage(textChannel, player.User.Mention + " only the host (" + ActiveGame.Host.User.Username + ") can start the game.");
                return;
            }else if (ActiveGame.WaitingList.Count < Game.MAXPLAYERS) { 
                await WriteMessage(textChannel, player.User.Mention + " you need 8 players to start the game.");
                return;
            }

            // If the game sucessfully started
            await textChannel.SendMessageAsync(ActiveGame.GameName + " has been started by " + player.PlayerMMRString(League.LeagueID) + ".");
            StartGame();
            // Prepare Blue Team
            String blueTeam = "Blue Team (" + ActiveGame.GetTeamMMR(Teams.Blue) + "): ";
            foreach (var p in ActiveGame.BlueList)
            {
                blueTeam += player.User.Mention + "(" + player.GetMMR(League.LeagueID) + ") ";   
            }

            // Prepare Red Team
            String redTeam = "Red Team (" + ActiveGame.GetTeamMMR(Teams.Red) + "): ";
            foreach (var p in ActiveGame.RedList)
            {
                redTeam += player.User.Mention + "(" + player.GetMMR(League.LeagueID) + ") ";
            }

            // Broadcast teams and password
            await textChannel.SendMessageAsync(blueTeam);
            await textChannel.SendMessageAsync(redTeam);
            await textChannel.SendMessageAsync("Password: " + Game.GeneratePassword(6));
        }

        /// <summary>
        /// Cancels the currently active game. Only the host can use this.
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to.</param>
        /// <param name="player">User who called the function.</param>
        public async Task cancelGame(IMessageChannel textChannel, Player player)
        {
            if (LobbyExists())
            {
                await textChannel.SendMessageAsync("No games open. Type !hostgame to create a game.");
                return;
            }
             
            if (player == ActiveGame.Host || player.User.RoleIds.Contains(_server.ModeratorRoles.Id) || player.User.GuildPermissions.Administrator)
            {
                await textChannel.SendMessageAsync("Game " + ActiveGame.GameName + " canceled by host " + player.User.Username + ".");     
                CancelGame();
            } 
        }

        /// <summary>
        /// Records the vote and anmnounces a winner if one is determined.
        /// </summary>
        /// <param name="player">User who voted.</param>
        /// <param name="team">Team that the player voted for.</param>
        public async Task voteCancel(IMessageChannel textChannel, Player player)
        {
            Console.WriteLine("!vc");
            if (ActiveGame == null)
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            if (!ActiveGame.WaitingList.Contains(player)) { 
                await WriteMessage(textChannel, player.User.Mention + " only players who were in the game can vote.");
                return;
            }
            if (ActiveGame.CancelCalls.Contains(player)) { 
                await WriteMessage(textChannel, player.User.Mention + " you have already voted.");
                return;
            }

            ActiveGame.CancelCalls.Add(player);
            if (ActiveGame.CancelCalls.Count >= Math.Floor((double) ActiveGame.WaitingList.Count()/2))
            {
                await textChannel.SendMessageAsync("Vote recorded to cancel game by " + player.User.Username + " (" + ActiveGame.CancelCalls.Count() + "/"+(int)(ActiveGame.WaitingList.Count/2)+")");
            }
            else
            {
                await textChannel.SendMessageAsync("Game " + ActiveGame.GameName + " canceled by vote.");
                CancelGame();
            }   
        }
        /// <summary>
        /// Lists all of the players in the currently ActiveGame
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to.</param>
        public async Task listPlayers(IMessageChannel textChannel)
        {
            if (ActiveGame == null)
            {
                await WriteMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            String message = ActiveGame.GameName + " (" + ActiveGame.WaitingList.Count() + "/8) lobby list: \n";
            foreach (var user in ActiveGame.WaitingList)
            {
                message += user.PlayerMMRString(League.LeagueID) + " ";
            }
            await WriteMessage(textChannel, message);
        }

        /// <summary>
        /// Records the vote and anmnounces a winner if one is determined.
        /// </summary>
        /// <param name="e">MessageEventArgs, used to extract gameName and Channel</param>
        /// <param name="player">User who voted.</param>
        /// <param name="team">Team that the player voted for.</param>
        public async Task voteWinner(IMessageChannel textChannel, Player player, Teams team)
        {
            if (!player.IsIngame())
            {
                await WriteMessage(textChannel, player.User.Mention + " you are not in a game.");
                return;
            }

            Game game = player.CurrentGame;
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
                    await textChannel.SendMessageAsync("Vote recorded for Red team in game " + game.GameName + " by " + player.User.Username + ". (" + game.RedWinCalls.Count() + "/5)");
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
                    await textChannel.SendMessageAsync("Vote recorded for Blue team in game " + game.GameName + " by " + player.User.Username + ". (" + game.BlueWinCalls.Count() + "/5)");
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
                        await textChannel.SendMessageAsync("Vote recorded for Red team in game " + game.GameName + " by " + player.User.Username + ". (" + game.RedWinCalls.Count() + "/5)");
                        break;
                    case Teams.Blue:
                        game.BlueWinCalls.Add(player);
                        await textChannel.SendMessageAsync("Vote recorded for Blue team in game " + game.GameName + " by " + player.User.Username + ". (" + game.BlueWinCalls.Count() + "/5)");
                        break;
                    case Teams.Draw:
                        break;
                }
            }

            if (game.BlueWinCalls.Count == Game.VOTETHRESHOLD)
            {
               EndGame(game, Teams.Blue, textChannel);
            }
            if (game.RedWinCalls.Count == Game.VOTETHRESHOLD) {
               EndGame(game, Teams.Red, textChannel);
            }
            if (game.DrawCalls.Count == Game.VOTETHRESHOLD) {
               EndGame(game, Teams.Draw, textChannel);
            }
        }

        /// <summary>
        /// Prints a list of all the games currently open/in progress.
        /// </summary>
        /// <param name="textChannel">Channel to send the message to.</param>
        public async Task getGames(IMessageChannel textChannel)
        {
            if (ActiveGame != null)
                await WriteMessage(textChannel, "Game in lobby: " + ActiveGame.GameName + " (" + ActiveGame.WaitingList.Count() + "/8)");
            else
                await WriteMessage(textChannel, "No games in lobby.");

            if (RunningGames.Count > 0)
            {
                String message = "Games in progress: ";
                foreach (var game in RunningGames)
                {
                    message += game.GameName + " ";
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
        public async Task getStats(IMessageChannel textChannel, Player player)
        {
            if (player != null)
            {
                int wins = player.GetWins(League.LeagueID);
                int losses = player.GetLosses(League.LeagueID);
                int gamesPlayed = wins + losses;
                await WriteMessage(textChannel, player.PlayerMMRString(League.LeagueID) + " has " + gamesPlayed + " games played, " + wins + " wins, " + losses + " losses.\nCurrent win streak: " + player.GetStreak(League.LeagueID) + ".");
            }
            else
                await WriteMessage(textChannel, player.User.Username + " has no recorded stats.");
        }

        /// <summary>
        /// Broadcasts the top 5 players and their MMR to the text channel.
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to</param>
        /// <param name="ds">Database object</param>
        public async Task getTopMMR(IMessageChannel textChannel)
        {
            // Sort dictionary by MMR
            var sortedDict = from entry in League.RegisteredPlayers orderby entry.GetMMR(League.LeagueID) descending select entry ;

            await WriteMessage(textChannel, "Top 5 players by MMR:");
            string message = "";
            int i = 0;
            foreach (var obj in sortedDict)
            {
                if (i == 5)
                    break;
                message += "#" + (i + 1) + " " + obj.PlayerMMRString(League.LeagueID) + ", ";
                i++;
            }
            await WriteMessage(textChannel, message);
        }

        /// <summary>
        /// Sends the helpString to a player in a private message.
        /// </summary>
        /// <param name="user">User to send message to.</param>
        //public void printHelp(Discord.User user)
        //{
        //    user.SendMessage(String.Format("test", Assembly.GetExecutingAssembly().GetName().Version.PlayerMMRString(League.LeagueID)()));
        //}

        /// <summary>
        /// Ends the game.
        /// </summary>
        /// <param name="textChannel">Text channel to broadcast to.</param>
        /// <param name="gameName">Name of the game to end.</param>
        public async void EndGame(Game game, Teams team, IMessageChannel textChannel) {
            CloseGame(game,team);
            await textChannel.SendMessageAsync("Closing game " + game.GameName);
            printGameResult(game, team, textChannel);
        }

        private async void printGameResult(Game game, Teams winnerTeam, IMessageChannel textChannel)
        {
            switch (winnerTeam) {
                case Teams.Red:
                    await textChannel.SendMessageAsync("Red team has won " + game.GameName + "!");
                    await textChannel.SendMessageAsync(GetGameResultString(game, Teams.Red));
                    break;
                case Teams.Blue:
                    await textChannel.SendMessageAsync("Blue team has won " + game.GameName + "!");
                    await textChannel.SendMessageAsync(GetGameResultString(game, Teams.Blue));

                    break;
                case Teams.Draw:
                    await textChannel.SendMessageAsync("Game " + game.GameName + " has ended in a draw. No stats have been recorded.");
                    break;
            }

        }

        private String GetGameResultString(Game game, Teams winner)
        {
            char blueSign = '+';
            char redSign = '+';
            if (Teams.Blue == winner)
                redSign = '-';
            else
                blueSign = '-';

            String message = "";
            message += "Blue team (" + blueSign + game.MmrAdjustment + "): ";
            foreach (var player in game.BlueList) {
                if (player.GetStreak(League.LeagueID) > 1)
                    message += player.PlayerMMRString(League.LeagueID) + "+" + 2 * (player.GetStreak(League.LeagueID) - 1) + " ";
                else
                    message += player.PlayerMMRString(League.LeagueID) + " ";
            }
            message += "\n";
            message += "Red team ("+ redSign + game.MmrAdjustment + "): ";
            foreach (var player in game.RedList) {
                if (player.GetStreak(League.LeagueID) > 1)
                    message += player.PlayerMMRString(League.LeagueID) + "+" + 2 * (player.GetStreak(League.LeagueID) - 1) + " ";
                else
                    message += player.PlayerMMRString(League.LeagueID) + " ";
            }

            return message;
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
