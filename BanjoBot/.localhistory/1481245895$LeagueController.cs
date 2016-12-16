using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Reflection;
using System.Resources;

namespace BanjoBot
{
    class LeagueController
    {
        private List<League> _leagues;
        private readonly Server _server;
        private readonly DataStore _dataStore;
        private readonly Role _moderatoRole;
        private readonly Role _adminRole;

        public LeagueController(Server server, DataStore ds)
        {
            _dataStore = ds;
            _server = server;
            _leagues = new List<League>();
            CreateLeagues();
            _moderatoRole =_server.FindRoles("League Moderator", true).First();
            _adminRole =_server.FindRoles("Server Admin", true).First();
        }


        private void CreateLeagues()
        {
            var role = _server.FindRoles("EU-BBL", true).First();
            var channel = _server.FindChannels("eu-bbl", null, true).First();
            _leagues.Add(new League("EU-BBL", channel,role));
            role = _server.FindRoles("AUS-BBL", true).First();
            channel = _server.FindChannels("aus-bbl", null, true).First();
            _leagues.Add(new League("AUS-BBL", channel, role));
            role = _server.FindRoles("NA-BBL", true).First();
            channel = _server.FindChannels("na-bbl", null, true).First();
            _leagues.Add(new League("NA-BBL",channel, role));
        }

        public List<League> GetLeagues()
        {
            return _leagues;
        } 

        /// <summary>
        /// Creates a new Game, binding the host and broadcasting to the Channel.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="host">The User who hosted the game.</param>
        public async Task hostGame(Channel textChannel, Player host)
        {
            League league = GetLeagueByChannel(textChannel);

            if (host.IsIngame())
            {
                await writeMessage(textChannel, host.Mention + " Vote before hosting another game");
                return;
            }

            // If no games are open.
            if (league.LobbyExists())
            {
                
                Game newGame = league.HostGame(host);
                await textChannel.SendMessage("New game " + newGame.GameName + " hosted by " + host.ToString() + ". \nType !join to join the game. (" + newGame.WaitingList.Count() + "/8)");
            } else { 
                await writeMessage(textChannel, host.Mention + " Game " + league.GetActiveGame().GameName + " is already open. Only one game may be hosted at a time. \nType !join to join the game.");
            }
        }

        public League GetLeagueByChannel(Channel channel)
        {
            return _leagues.FirstOrDefault(league => league.Channel == channel);
        }

        /// <summary>
        /// Adds a User to the currently active Game (if one exists) and broadcasts to the Channel.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="player">User who wishes to join.</param>
        public async Task joinGame(Channel textChannel, Player player)
        {
            League league = GetLeagueByChannel(textChannel);  
            if (player.IsIngame())
            {
                await writeMessage(textChannel, player.Mention + " Vote before joining another game");
                return;
            } 
            if (league.LobbyExists())
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to add player
            bool? addPlayerResult = league.GetActiveGame().addPlayer(player);

            // If successful
            if (addPlayerResult == true)
            {
                await textChannel.SendMessage(player.ToString() + " has joined " + league.GetActiveGame().GameName + ". (" + league.GetActiveGame().WaitingList.Count() + "/8)");
                if (league.GetActiveGame().WaitingList.Count() == 8) { 
                    await textChannel.SendMessage(league.GetActiveGame().Host.Mention + ", The lobby is full. Type !startgame to start the game");
                }
            }

            // If unsuccessfull
            else if (addPlayerResult == false)
                await writeMessage(textChannel, player.Mention + " The game " + league.GetActiveGame().GameName + " is full.");
            // If player already in game
            else if (addPlayerResult == null)
                await writeMessage(textChannel, player.Mention + " you can not join a game you are already in.");
            else
                await writeMessage(textChannel, "Error: Command.joinGame()");
        }

        /// <summary>
        /// Removes a User from the currently active Game (if one exists) and broadcasts to the Channel.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="user">User who wishes to leave.</param>
        public async Task leaveGame(Channel textChannel, Player user)
        {
            League league = GetLeagueByChannel(textChannel);
            // If no games are open.
            if (league.LobbyExists())
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to remove player
            bool? removePlayerResult = league.GetActiveGame().removePlayer(user);

            // If successful
            if (removePlayerResult == true)
                await textChannel.SendMessage(user.ToString() + " has left " + league.GetActiveGame().GameName + ". (" + league.GetActiveGame().WaitingList.Count() + "/8)");
            // If game now empty
            else if (removePlayerResult == false)
            {
                await writeMessage(textChannel, user.ToString() + " has left " + league.GetActiveGame().GameName + ". (" + league.GetActiveGame().WaitingList.Count() + "/8)");
                await writeMessage(textChannel,"Closing game " + league.GetActiveGame().GameName);
                league.CancelGame();
            }
            // If player not in game
            else if (removePlayerResult == null)
                await writeMessage(textChannel, user.Mention + " you are not in this game.");
            else
                await writeMessage(textChannel, "Error: Command.leaveGame()");
        }

        /// <summary>
        /// Starts the currently active game. Only the host can use this command sucessfully.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="playerUser who wishes to leave.</param>
        public async Task startGame(Channel textChannel, Player player)
        {
            League league = GetLeagueByChannel(textChannel);
            Game activeGame = league.GetActiveGame();
            if (!league.LobbyExists())
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // If the player who started the game was not the host
            if (activeGame.Host != player) { 
                await writeMessage(textChannel, player.Mention + " only the host (" + activeGame.Host.Name + ") can start the game.");
                return;
            }else if (activeGame.WaitingList.Count < Game.MAXPLAYERS) { 
                await writeMessage(textChannel, player.Mention + " you need 8 players to start the game.");
                return;
            }

            // If the game sucessfully started
            await textChannel.SendMessage(league.GetActiveGame().GameName + " has been started by " + player.ToString() + ".");
            league.StartGame();
            // Prepare Blue Team
            String blueTeam = "Blue Team (" + activeGame.getTeamMMR(Teams.Blue) + "): ";
            foreach (var p in activeGame.BlueList)
            {
                blueTeam += player.Mention + "(" + player.Mmr + ") ";   
            }

            // Prepare Red Team
            String redTeam = "Red Team (" + activeGame.getTeamMMR(Teams.Red) + "): ";
            foreach (var p in activeGame.RedList)
            {
                redTeam += player.Mention + "(" + player.Mmr + ") ";
            }

            // Broadcast teams and password
            await textChannel.SendMessage(blueTeam);
            await textChannel.SendMessage(redTeam);
            await textChannel.SendMessage("Password: " + Game.GeneratePassword(6));
        }

        /// <summary>
        /// Cancels the currently active game. Only the host can use this.
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to.</param>
        /// <param name="player">User who called the function.</param>
        public async Task cancelGame(Channel textChannel, Player player)
        {
            League league = GetLeagueByChannel(textChannel);
            if (league.LobbyExists())
            {
                await textChannel.SendMessage("No games open. Type !hostgame to create a game.");
                return;
            }

            if (player == league.GetActiveGame().Host || player.User.HasRole(_moderatoRole) || player.User.HasRole(_adminRole))
            {
                await textChannel.SendMessage("Game " + league.GetActiveGame().GameName + " canceled by host " + player.Name + ".");     
                league.CancelGame();
            } 
        }

        /// <summary>
        /// Records the vote and anmnounces a winner if one is determined.
        /// </summary>
        /// <param name="player">User who voted.</param>
        /// <param name="team">Team that the player voted for.</param>
        public async Task voteCancel(Channel textChannel, Player player)
        {
            League league = GetLeagueByChannel(textChannel);
            Game activeGame = league.GetActiveGame();
            if (activeGame == null)
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            if (!activeGame.WaitingList.Contains(player)) { 
                await writeMessage(textChannel, player.Mention + " only players who were in the game can vote.");
                return;
            }
            if (activeGame.CancelCalls.Contains(player)) { 
                await writeMessage(textChannel, player.Mention + " you have already voted.");
                return;
            }

            activeGame.CancelCalls.Add(player);
            if (activeGame.CancelCalls.Count >= Math.Floor((double) activeGame.WaitingList.Count()/2))
            {
                await textChannel.SendMessage("Vote recorded to cancel game by " + player.Name + " (" + activeGame.CancelCalls.Count() + "/"+(int)(activeGame.WaitingList.Count/2)+")");
            }
            else
            {
                await textChannel.SendMessage("Game " + activeGame.GameName + " canceled by vote.");
                league.CancelGame();
            }   
        }
        /// <summary>
        /// Lists all of the players in the currently activeGame
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to.</param>
        public async Task listPlayers(Channel textChannel)
        {
            League league = GetLeagueByChannel(textChannel);
            Game activeGame = league.GetActiveGame();
            if (activeGame == null)
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            String message = activeGame.GameName + " (" + activeGame.WaitingList.Count() + "/8) lobby list: \n";
            foreach (var user in activeGame.WaitingList)
            {
                message += user.ToString() + " ";
            }
            await writeMessage(textChannel, message);
        }

        /// <summary>
        /// Records the vote and anmnounces a winner if one is determined.
        /// </summary>
        /// <param name="e">MessageEventArgs, used to extract gameName and Channel</param>
        /// <param name="player">User who voted.</param>
        /// <param name="team">Team that the player voted for.</param>
        public async Task voteWinner(Channel textChannel, Player player, Teams team)
        {
            if (!player.IsIngame())
            {
                await writeMessage(textChannel, player.Mention + " you are not in a game.");
                return;
            }

            Game game = player.CurrentGame;
            if (game.BlueWinCalls.Contains(player))
            {
                if (team == Teams.Blue || team == Teams.Draw)
                {
                    await writeMessage(textChannel, player.Mention + " you have already voted for this team.");
                }
                else if (team == Teams.Red)
                {
                    game.BlueWinCalls.Remove(player);
                    game.RedWinCalls.Add(player);
                    await writeMessage(textChannel, player.Mention + " has changed his Mind");
                    await textChannel.SendMessage("Vote recorded for Red team in game " + game.GameName + " by " + player.Name + ". (" + game.RedWinCalls.Count() + "/5)");
                }
            }
            else if (game.RedWinCalls.Contains(player))
            {
                if (team == Teams.Red || team == Teams.Draw) {
                    await writeMessage(textChannel, player.Mention + " you have already voted for this team.");
                }
                else if (team == Teams.Blue) {
                    game.RedWinCalls.Remove(player);
                    game.BlueWinCalls.Add(player);
                    await writeMessage(textChannel, player.Mention + " has changed his Mind");
                    await textChannel.SendMessage("Vote recorded for Blue team in game " + game.GameName + " by " + player.Name + ". (" + game.BlueWinCalls.Count() + "/5)");
                }
            }
            else if (game.DrawCalls.Contains(player))
            {
                await writeMessage(textChannel, player.Mention + " you have already voted");
            }
            else
            {
                switch (team)
                {
                    case Teams.Red:
                        game.RedWinCalls.Add(player);
                        await textChannel.SendMessage("Vote recorded for Red team in game " + game.GameName + " by " + player.Name + ". (" + game.RedWinCalls.Count() + "/5)");
                        break;
                    case Teams.Blue:
                        game.BlueWinCalls.Add(player);
                        await textChannel.SendMessage("Vote recorded for Blue team in game " + game.GameName + " by " + player.Name + ". (" + game.BlueWinCalls.Count() + "/5)");
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
        public async Task getGames(Channel textChannel)
        {
            League league = GetLeagueByChannel(textChannel);
            Game activeGame = league.GetActiveGame();
            if (activeGame != null)
                await writeMessage(textChannel, "Game in lobby: " + activeGame.GameName + " (" + activeGame.WaitingList.Count() + "/8)");
            else
                await writeMessage(textChannel, "No games in lobby.");

            if (league.GetRunningGames().Count > 0)
            {
                String message = "Games in progress: ";
                foreach (var game in league.GetRunningGames())
                {
                    message += game.GameName + " ";
                }
                await writeMessage(textChannel, message);
            }
            else
                await writeMessage(textChannel, "No games in progress.");
        }

        /// <summary>
        /// Prints a users stats to the text channel.
        /// </summary>
        /// <param name="textChannel">Channel to send message to.</param>
        /// <param name="user">Users whos stats will be displayed.</param>
        public async Task getStats(Channel textChannel, DataStore ds, String playerName)
        {
            ulong playerID = Convert.ToUInt64( playerName.Trim(new char[] { '<', '@', '!', '>' }) );

            Player user = ds.getPlayer(playerID);
            if (user != null)
            {
                int wins = user.Wins;
                int losses = user.Losses;
                int gamesPlayed = wins + losses;
                float winRate = wins/gamesPlayed;
                await writeMessage(textChannel, user.ToString() + " has " + gamesPlayed + " games played, " + wins + " wins, " + losses + " losses.\nWinrate: " + winRate.ToString("p") + " \nCurrent win streak: " + user.Streak + ".");
            }
            else
                await writeMessage(textChannel, playerName + " has no recorded stats.");
        }

        /// <summary>
        /// Broadcasts the top 5 players and their MMR to the text channel.
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to</param>
        /// <param name="ds">Database object</param>
        public async Task getTopMMR(Channel textChannel, DataStore ds)
        {
            // Sort dictionary by MMR
            var sortedDict = from entry in ds.users orderby entry.Value.Mmr descending select entry;

            await writeMessage(textChannel, "Top 5 players by MMR:");
            string message = "";
            int i = 0;
            foreach (var obj in sortedDict)
            {
                if (i == 5)
                    break;
                message += "#" + (i + 1) + " " + obj.Value.ToString() + ", ";
                i++;
            }
            await writeMessage(textChannel, message);
        }

        /// <summary>
        /// Broadcasts the top 5 players and their MMR to the text channel (sorted based on games played).
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to</param>
        /// <param name="ds">Database object</param>
        public async Task getTopGames(Channel textChannel, DataStore ds)
        {
            // TODO: Sort dictionary by Games played
            var sortedDict = from entry in ds.users orderby entry.Value.Mmr descending select entry;            // TODO: Add games played count to users, sort by games played.

            await writeMessage(textChannel, "Top 5 players by games played:");
            string message = "";
            int i = 0;
            foreach (var obj in sortedDict)
            {
                if (i == 5)
                    break;
                message += "#" + (i + 1) + " " + obj.Value.ToString() + ", ";
                i++;
            }
            await writeMessage(textChannel, message);
        }

        /// <summary>
        /// Saves the current gameCounter and User data.
        /// </summary>
        /// <param name="textChannel"></param>
        public void saveData(ulong id)
        {
            // If caller is author or admin
            if (id == 118233869093699588 || id == 118288095790628871)
            {
                DataStore.GetInstance().writeXML();
                Console.Out.WriteLine("Data saved.");
            }
        }

        /// <summary>
        /// Sends the helpString to a player in a private message.
        /// </summary>
        /// <param name="user">User to send message to.</param>
        public void printHelp(Discord.User user)
        {
            user.SendMessage(String.Format("test", Assembly.GetExecutingAssembly().GetName().Version.ToString()));
        }

        /// <summary>
        /// Ends the game.
        /// </summary>
        /// <param name="textChannel">Text channel to broadcast to.</param>
        /// <param name="gameName">Name of the game to end.</param>
        public async void EndGame(Game game, Teams team, Channel textChannel) {
            League league = GetLeagueByChannel(textChannel);
            league.CloseGame(game,team);
            await textChannel.SendMessage("Closing game " + game.GameName);
            printGameResult(game, team, textChannel);
        }

        private async void printGameResult(Game game, Teams winnerTeam, Channel textChannel)
        {
            switch (winnerTeam) {
                case Teams.Red:
                    await textChannel.SendMessage("Red team has won " + game.GameName + "!");
                    await textChannel.SendMessage(GetGameResultString(game, Teams.Red));
                    break;
                case Teams.Blue:
                    await textChannel.SendMessage("Blue team has won " + game.GameName + "!");
                    await textChannel.SendMessage(GetGameResultString(game, Teams.Blue));

                    break;
                case Teams.Draw:
                    await textChannel.SendMessage("Game " + game.GameName + " has ended in a draw. No stats have been recorded.");
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
                if (player.Streak > 1)
                    message += player.ToString() + "+" + 2 * (player.Streak - 1) + " ";
                else
                    message += player.ToString() + " ";
            }
            message += "\n";
            message += "Red team ("+ redSign + game.MmrAdjustment + "): ";
            foreach (var player in game.RedList) {
                if (player.Streak > 1)
                    message += player.ToString() + "+" + 2 * (player.Streak - 1) + " ";
                else
                    message += player.ToString() + " ";
            }

            return message;
        }

        /// <summary>
        /// Searches the runningGames list for a game with name = gameName.
        /// <para> Returns index if true, -1 if false.</para>
        /// </summary>
        /// <param name="gameName">Name of the game to search.</param>
        /// <returns>Returns index if true, -1 if false.</returns>
        private int gameExists (String gameName)
        {


            int result = -1;
            
            for (int i=0; i<runningGames.Count(); i++)
            {
                if (runningGames[i].gameName == gameName)
                    return i;
            }

            return result;
        }

        /// <summary>
        /// Used to send temporary messages. Sends a message to the Channel then deletes the message after a delay.
        /// </summary>
        /// <param name="textChannel">Channel to send the message to</param>
        /// <param name="message">Message to be sent to the channel</param>
        /// <returns></returns>
        private async Task writeMessage(Channel textChannel, String message)
        {
            // Send message
            Message discordMessage = await textChannel.SendMessage(message);

            // Delete message
            System.Threading.ThreadPool.QueueUserWorkItem(delegate {
                deleteMessage(discordMessage);
            }, null);
        }

        /// <summary>
        /// Deletes the message after a delay.
        /// </summary>
        /// <param name="message"></param>
        private void deleteMessage(Message message)
        {
            System.Threading.Thread.Sleep(20 * 1000);
            message.Delete();
        }
    }
}
