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

        public LeagueController(Server server)
        {
            this._server = server;
            _leagues = new List<League>();
            CreateLeagues();
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

            //if(host.user.HasRole(getR))

            if(league == null) {
                String leagueNames = "";
                foreach (League l in _leagues) {
                    leagueNames += l.Channel.Mention + " ";
                }
                await textChannel.SendMessage("This is no league channel. Try it here " + leagueNames + "again");
            }

            bool? inGame = gameCheck(host);
            if (inGame == false)
            {
                await writeMessage(textChannel, host.mention + " Vote before hosting another game");
                return;
            }
            // If no games are open.
            if (!openGamesExists(textChannel))
            {
                Game newGame = new Game(host, textChannel);
                activeGames.Add(newGame);
                await textChannel.SendMessage("New game " + newGame.gameName + " hosted by " + host.ToString() + ". \nType !join to join the game. (" + newGame.waitingList.Count() + "/8)");
            } else { 
                await writeMessage(textChannel, host.mention + " Game " + newGame.gameName + " is already open. Only one game may be hosted at a time. \nType !join to join the game.");
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
        /// <param name="user">User who wishes to join.</param>
        public async Task joinGame(Channel textChannel, Player user)
        {
            bool? inGame = gameCheck(user);
            if (inGame == false)
            {
                await writeMessage(textChannel, user.mention + " Vote before joining another game");
                return;
            }
            // If no games are open.
            if (activeGame == null)
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to add player
            bool? addPlayerResult = activeGame.addPlayer(user);

            // If successful
            if (addPlayerResult == true)
            {
                await textChannel.SendMessage(user.ToString() + " has joined " + activeGame.gameName + ". (" + activeGame.waitingList.Count() + "/8)");
                if (activeGame.waitingList.Count() == 8) { 
                    await textChannel.SendMessage(activeGame.host.mention + ", The lobby is full. Type !startgame to start the game");
                }
            }
            // If unsuccessfull
            else if (addPlayerResult == false)
                await writeMessage(textChannel, user.mention + " The game " + activeGame.gameName + " is full.");
            // If user already in game
            else if (addPlayerResult == null)
                await writeMessage(textChannel, user.mention + " you can not join a game you are already in.");
            else
                await writeMessage(textChannel, "Error: Command.joinGame()");
        }

        private bool gameCheck(Player user)
        {
            string gameName = user.currentGame;
            int index = gameExists(gameName);
            if (index == -1)
                return true;
            if (runningGames[index].blueWinCalls.Contains(user) || runningGames[index].redWinCalls.Contains(user) || runningGames[index].drawCalls.Contains(user))
                return true;
            return false;
        }

        /// <summary>
        /// Removes a User from the currently active Game (if one exists) and broadcasts to the Channel.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="user">User who wishes to leave.</param>
        public async Task leaveGame(Channel textChannel, Player user)
        {
            // If no games are open.
            if (activeGame == null)
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to remove player
            bool? removePlayerResult = activeGame.removePlayer(user);

            // If successful
            if (removePlayerResult == true)
                await textChannel.SendMessage(user.ToString() + " has left " + activeGame.gameName + ". (" + activeGame.waitingList.Count() + "/8)");
            // If game now empty
            else if (removePlayerResult == false)
            {
                await writeMessage(textChannel, user.ToString() + " has left " + activeGame.gameName + ". (" + activeGame.waitingList.Count() + "/8)");
                this.endGame(textChannel, "active");
            }
            // If user not in game
            else if (removePlayerResult == null)
                await writeMessage(textChannel, user.mention + " you are not in this game.");
            else
                await writeMessage(textChannel, "Error: Command.leaveGame()");
        }

        /// <summary>
        /// Starts the currently active game. Only the host can use this command sucessfully.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="user">User who wishes to leave.</param>
        public async Task startGame(Channel textChannel, Player user)
        {
            // If no games are open.
            if (activeGame == null)
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to start the game.
            String startGameResult = activeGame.startGame(textChannel, user);

            // If the user who started the game was not the host
            if (startGameResult == "Not host")
                await writeMessage(textChannel, user.mention + " only the host (" + activeGame.host.name + ") can start the game.");
            else if (startGameResult == "Not enough players")
                await writeMessage(textChannel, user.mention + " you need 8 players to start the game.");
            // If the game failed to start
            else if (startGameResult == "Fail")
                await writeMessage(textChannel, "Game failed to start.");
            // If the game sucessfully started
            else
            {
                await textChannel.SendMessage(activeGame.gameName + " has been started by " + user.ToString() + ".");

                // Prepare Blue Team
                String blueTeam = "Blue Team (" + activeGame.getTeamMMR(Teams.Blue) + "): ";
                foreach (var player in activeGame.blueList)
                {
                    blueTeam += player.mention + "(" + player.mmr + ") ";
                    player.currentGame = activeGame.gameName;
                }

                // Prepare Red Team
                String redTeam = "Red Team (" + activeGame.getTeamMMR(Teams.Red) + "): ";
                foreach (var player in activeGame.redList)
                {
                    redTeam += player.mention + "(" + player.mmr + ") ";
                    player.currentGame = activeGame.gameName;
                }

                // Broadcast teams and password
                await textChannel.SendMessage(blueTeam);
                await textChannel.SendMessage(redTeam);
                await textChannel.SendMessage("Password: " + startGameResult);

                // Move game to running games.
                runningGames.Add(activeGame);
                activeGame = null;
            }

        }

        /// <summary>
        /// Cancels the currently active game. Only the host can use this.
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to.</param>
        /// <param name="user">User who called the function.</param>
        public async Task cancelGame(Channel textChannel, Player user)
        {
            // If no games are open.
            if (activeGame == null)
            {
                await textChannel.SendMessage("No games open. Type !hostgame to create a game.");
                return;
            }

            if (user == activeGame.host)
            {
                await textChannel.SendMessage("Game " + activeGame.gameName + " canceled by host " + user.name + ".");     
            } else if (user.id == 132875210394173440) {
                await textChannel.SendMessage("Game " + activeGame.gameName + " canceled by admin " + user.name + ".");
            }

            activeGame = null;
        }

        /// <summary>
        /// Records the vote and anmnounces a winner if one is determined.
        /// </summary>
        /// <param name="e">MessageEventArgs, used to extract gameName and Channel</param>
        /// <param name="user">User who voted.</param>
        /// <param name="team">Team that the user voted for.</param>
        public async Task voteCancel(Channel textChannel, Player user)
        {
            if (activeGame == null)
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            switch (activeGame.voteCancel(user))
            {
                case "not in game":
                    await writeMessage(textChannel, user.mention + " only players who were in the game can vote.");
                    break;

                case "already voted":
                    await writeMessage(textChannel, user.mention + " you have already voted.");
                    break;

                case "more votes":
                    await textChannel.SendMessage("Vote recorded to cancel game by " + user.name + " (" + activeGame.cancelCalls.Count() + "/5)");
                    break;

                case "canceled":
                    await textChannel.SendMessage("Game " + activeGame.gameName + " canceled by vote.");
                    activeGame = null;
                    break;
            }
        }
        /// <summary>
        /// Lists all of the players in the currently activeGame
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to.</param>
        public async Task listPlayers(Channel textChannel)
        {
            // If no games are open.
            if (activeGame == null)
            {
                await writeMessage(textChannel, "No games open. Type !hostgame to create a game.");
                return;
            }

            String message = activeGame.gameName + " (" + activeGame.waitingList.Count() + "/8) lobby list: \n";
            foreach (var user in activeGame.waitingList)
            {
                message += user.ToString() + " ";
            }
            //await writeMessage(textChannel, message);
            await writeMessage(textChannel, message);
        }

        /// <summary>
        /// Records the vote and anmnounces a winner if one is determined.
        /// </summary>
        /// <param name="e">MessageEventArgs, used to extract gameName and Channel</param>
        /// <param name="user">User who voted.</param>
        /// <param name="team">Team that the user voted for.</param>
        public async Task voteWinner(Channel textChannel, Player user, Teams team)
        {
            string gameName = user.currentGame;
            int index = gameExists(gameName);

            // If the game doesn't exist.
            if (index == -1)
            {
                await writeMessage(textChannel, user.mention + " you are not in a game.");
                return;
            }
            String result = runningGames[index].recordVote(user, team);
            switch (result)
            {
                case "not in game":
                    await writeMessage(textChannel, user.mention + " only players who were in the game can vote.");
                    break;

                case "already voted":
                    await writeMessage(textChannel, user.mention + " you have already voted for this team.");
                    break;

                case "more votes":
                    if (team == Teams.Blue)
                        await textChannel.SendMessage("Vote recorded for Blue team in game " + gameName + " by " + user.name + ". (" + runningGames[index].blueWinCalls.Count() + "/5)");
                    else if (team == Teams.Red)
                        await textChannel.SendMessage("Vote recorded for Red team in game " + gameName + " by " + user.name + ". (" + runningGames[index].redWinCalls.Count() + "/5)");
                    else
                        await textChannel.SendMessage("Vote recorded for draw in game " + gameName + " by " + user.name + ". (" + runningGames[index].drawCalls.Count() + "/5)");
                    break;

                case "blue":
                    await textChannel.SendMessage("Blue team has won " + gameName + "!");

                    // Print Blue team MMR
                    String messageBlue = "Blue team (+" + runningGames[index].mmrAdjustment + "): ";
                    foreach (var player in runningGames[index].blueList)
                    {
                        if (player.streak > 1)
                            messageBlue += player.ToString() + "+" + 2 * (player.streak-1) + " ";
                        else
                            messageBlue += player.ToString() + " ";
                        player.currentGame = "0";
                    }
                    await textChannel.SendMessage(messageBlue);

                    // Print Red team MMR
                    messageBlue = "Red team (-" + runningGames[index].mmrAdjustment + "): ";
                    foreach (var player in runningGames[index].redList)
                    {
                        messageBlue += player.ToString() + " ";
                        player.currentGame = "0";
                    }
                    await textChannel.SendMessage(messageBlue);

                    // End the game
                    endGame(textChannel, gameName);
                    break;

                case "red":
                    await textChannel.SendMessage("Red team has won " + gameName + "!");

                    // Print Blue team MMR
                    String messageRed = "Blue team (-" + runningGames[index].mmrAdjustment + "): ";
                    foreach (var player in runningGames[index].blueList)
                    {
                        messageRed += player.ToString() + " ";
                        player.currentGame = "0";
                    }
                    await textChannel.SendMessage(messageRed);

                    // Print Red team MMR
                    messageRed = "Red team (+" + runningGames[index].mmrAdjustment + "): ";
                    foreach (var player in runningGames[index].redList)
                    {
                        if (player.streak > 1)
                            messageRed += player.ToString() + "+" + 2*(player.streak-1) + " ";
                        else
                            messageRed += player.ToString() + " ";
                        player.currentGame = "0";
                    }
                    await textChannel.SendMessage(messageRed);

                    // End the game
                    endGame(textChannel, gameName);
                    break;
                case "draw":
                    // No stats recorded
                    await textChannel.SendMessage("Game " + gameName + " has ended in a draw. No stats have been recorded.");
                    foreach (var player in runningGames[index].blueList)
                    {
                        player.currentGame = "0";
                    }
                    foreach (var player in runningGames[index].redList)
                    {
                        player.currentGame = "0";
                    }
                    // End the game
                    endGame(textChannel, gameName);
                    break;
            }
        }

        /// <summary>
        /// Prints a list of all the games currently open/in progress.
        /// </summary>
        /// <param name="textChannel">Channel to send the message to.</param>
        public async Task getGames(Channel textChannel)
        {
            if (activeGame != null)
                await writeMessage(textChannel, "Game in lobby: " + activeGame.gameName + " (" + activeGame.waitingList.Count() + "/8)");
            else
                await writeMessage(textChannel, "No games in lobby.");

            if (runningGames.Count > 0)
            {
                String message = "Games in progress: ";
                foreach (var game in runningGames)
                {
                    message += game.gameName + " ";
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
                int wins = user.wins;
                int losses = user.losses;
                int gamesPlayed = wins + losses;
                await writeMessage(textChannel, user.ToString() + " has " + gamesPlayed + " games played, " + wins + " wins, " + losses + " losses. \nCurrent win streak: " + user.streak + ".");
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
            var sortedDict = from entry in ds.users orderby entry.Value.mmr descending select entry;

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
            var sortedDict = from entry in ds.users orderby entry.Value.mmr descending select entry;            // TODO: Add games played count to users, sort by games played.

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
                Program.ds.writeXML();
                Console.Out.WriteLine("Data saved.");
            }
        }

        /// <summary>
        /// Sends the helpString to a user in a private message.
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
        public void endGame(Channel textChannel, String gameName)
        {
            if (gameName == "active")
            {
                textChannel.SendFile("Closing game " + activeGame.gameName);
                activeGame.endGame();
                activeGame = null;
            }
            else
            {
                int index = gameExists(gameName);
                if (index >= 0 && index < runningGames.Count())
                {
                    textChannel.SendMessage("Closing game " + gameName);
                    runningGames[index].endGame();
                    runningGames.Remove(runningGames[index]);
                }
            }
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
