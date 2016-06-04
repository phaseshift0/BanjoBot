using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;

namespace BanjoBot
{
    class Commands
    {
        // Props
        private Game activeGame;
        private List<Game> runningGames;

        public Commands()
        {
            activeGame   = null;
            runningGames = new List<Game>();
        }

        /// <summary>
        /// Creates a new Game, binding the host and broadcasting to the Channel.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="host">The User who hosted the game.</param>
        public void hostGame(Channel textChannel, User host)
        {
            // If no games are open.
            if (activeGame == null)
            {
                activeGame = new Game(host);
                textChannel.SendMessage("New game " + activeGame.gameName + " hosted by " + host.ToString() + ". \nType !join to join the game. (" + activeGame.waitingList.Count() + "/8)");
            }
            else
                textChannel.SendMessage("@" + host.name + " Game " + activeGame.gameName + " is already open. Only one game may be hosted at a time. \nType !join to join the game.");
        }

        /// <summary>
        /// Adds a User to the currently active Game (if one exists) and broadcasts to the Channel.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="user">User who wishes to join.</param>
        public void joinGame(Channel textChannel, User user)
        {
            // If no games are open.
            if (activeGame == null)
            {
                textChannel.SendMessage("No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to add player
            bool? addPlayerResult = activeGame.addPlayer(user);

            // If successful
            if (addPlayerResult == true)
                textChannel.SendMessage(user.ToString() + " has joined " + activeGame.gameName + ". (" + activeGame.waitingList.Count() + "/8)");
            // If unsuccessfull
            else if (addPlayerResult == false)
                textChannel.SendMessage("@" + user.name + " The game " + activeGame.gameName + " is full.");
            // If user already in game
            else if (addPlayerResult == null)
                textChannel.SendMessage("@" + user.name + " you can not join a game you are already in.");
            else
                textChannel.SendMessage("Error: Command.joinGame()");
        }

        /// <summary>
        /// Removes a User from the currently active Game (if one exists) and broadcasts to the Channel.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="user">User who wishes to leave.</param>
        public void leaveGame(Channel textChannel, User user)
        {
            // If no games are open.
            if (activeGame == null)
            {
                textChannel.SendMessage("No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to remove player
            bool? removePlayerResult = activeGame.removePlayer(user);

            // If successful
            if (removePlayerResult == true)
                textChannel.SendMessage(user.ToString() + " has left " + activeGame.gameName + ". (" + activeGame.waitingList.Count() + "/8)");
            // If game now empty
            else if (removePlayerResult == false)
            {
                textChannel.SendMessage(user.ToString() + " has left " + activeGame.gameName + ". (" + activeGame.waitingList.Count() + "/8)");
                this.endGame(textChannel, "active");
            }
            // If user not in game
            else if (removePlayerResult == null)
                textChannel.SendMessage("@" + user.name + " you are not in this game.");
            else
                textChannel.SendMessage("Error: Command.leaveGame()");
        }

        /// <summary>
        /// Starts the currently active game. Only the host can use this command sucessfully.
        /// </summary>
        /// <param name="textChannel">The Channel to be broadcasted to.</param>
        /// <param name="user">User who wishes to leave.</param>
        public void startGame(Channel textChannel, User user)
        {
            // If no games are open.
            if (activeGame == null)
            {
                textChannel.SendMessage("No games open. Type !hostgame to create a game.");
                return;
            }

            // Attempt to start the game.
            String startGameResult = activeGame.startGame(textChannel, user);

            // If the user who started the game was not the host
            if (startGameResult == "Not host")
                textChannel.SendMessage("@" + user.name + " only the host (" + activeGame.host.name + ") can start the game.");
            else if (startGameResult == "Not enough players")
                textChannel.SendMessage("@" + user.name + " you need 8 players to start the game.");
            // If the game failed to start
            else if (startGameResult == "Fail")
                textChannel.SendMessage("Game failed to start.");
            // If the game sucessfully started
            else
            {
                textChannel.SendMessage(activeGame.gameName + " has been started by " + user.ToString() + ".");

                // Prepare Blue Team
                String blueTeam = "Blue Team (" + activeGame.getTeamMMR(Teams.Blue) + "): ";
                foreach (var player in activeGame.blueList)
                {
                    blueTeam += player.ToString() + " ";
                }

                // Prepare Red Team
                String redTeam = "Red Team (" + activeGame.getTeamMMR(Teams.Red) + "): ";
                foreach (var player in activeGame.redList)
                {
                    redTeam += player.ToString() + " ";
                }
                    
                // Broadcast teams and password
                textChannel.SendMessage(blueTeam);
                textChannel.SendMessage(redTeam);
                textChannel.SendMessage("Password: " + startGameResult);

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
        public void cancelGame(Channel textChannel, User user)
        {
            // If no games are open.
            if (activeGame == null)
            {
                textChannel.SendMessage("No games open. Type !hostgame to create a game.");
                return;
            }

            if (user == activeGame.host)
            {
                textChannel.SendMessage("Game " + activeGame.gameName + " canceled by host " + user.name + ".");
                activeGame = null;
            }
        }

        /// <summary>
        /// Lists all of the players in the currently activeGame
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to.</param>
        public void listPlayers(Channel textChannel)
        {
            // If no games are open.
            if (activeGame == null)
            {
                textChannel.SendMessage("No games open. Type !hostgame to create a game.");
                return;
            }

            String message = activeGame.gameName + " (" + activeGame.waitingList.Count() + "/8) lobby list: \n";
            foreach (var user in activeGame.waitingList)
            {
                message += user.ToString() + " ";
            }
            textChannel.SendMessage(message);
        }

        /// <summary>
        /// Records the vote and anmnounces a winner if one is determined.
        /// </summary>
        /// <param name="e">MessageEventArgs, used to extract gameName and Channel</param>
        /// <param name="user">User who voted.</param>
        /// <param name="team">Team that the user voted for.</param>
        public void voteWinner(MessageEventArgs e, User user, Teams team)
        {
            // Extract important data
            Channel textChannel = e.Channel;
            String gameName = extractName(e.Message.RawText);
            int index = gameExists(gameName);

            // If the game doesn't exist.
            if (index == -1)
            {
                textChannel.SendMessage("@" + user.name + " game " + gameName + " does not exist.");
                return;
            }
            String result = runningGames[index].recordVote(user, team);
            switch (result)
            {
                case "not in game":
                    textChannel.SendMessage("@" + user.name + " only players who were in the game can vote.");
                    break;

                case "already voted":
                    textChannel.SendMessage("@" + user.name + " you have already voted.");
                    break;

                case "more votes":
                    if (team == Teams.Blue)
                        textChannel.SendMessage("Vote recorded for Blue team in game " + gameName + " by " + user.name + ". (" + runningGames[index].blueWinCalls.Count() + "/5)");
                    else
                        textChannel.SendMessage("Vote recorded for Red team in game " + gameName + " by " + user.name + ". (" + runningGames[index].redWinCalls.Count() + "/5)");
                    break;

                case "blue":
                    textChannel.SendMessage("Blue team has won " + gameName + "!");

                    // Print Blue team MMR
                    String messageBlue = "Blue team (+" + runningGames[index].mmrAdjustment + "): ";
                    foreach (var player in runningGames[index].blueList)
                    {
                        if (player.streak > 1)
                            messageBlue += player.ToString() + "+" + 2 * (player.streak-1) + " ";
                        else
                            messageBlue += player.ToString() + " ";
                    }
                    textChannel.SendMessage(messageBlue);

                    // Print Red team MMR
                    messageBlue = "Red team (-" + runningGames[index].mmrAdjustment + "): ";
                    foreach (var player in runningGames[index].redList)
                    {
                        messageBlue += player.ToString() + " ";
                    }
                    textChannel.SendMessage(messageBlue);

                    // End the game
                    endGame(textChannel, gameName);
                    break;

                case "red":
                    textChannel.SendMessage("Red team has won " + gameName + "!");

                    // Print Blue team MMR
                    String messageRed = "Blue team (-" + runningGames[index].mmrAdjustment + "): ";
                    foreach (var player in runningGames[index].blueList)
                    {
                        messageRed += player.ToString() + " ";
                    }
                    textChannel.SendMessage(messageRed);

                    // Print Red team MMR
                    messageRed = "Red team (+" + runningGames[index].mmrAdjustment + "): ";
                    foreach (var player in runningGames[index].redList)
                    {
                        if (player.streak > 1)
                            messageRed += player.ToString() + "+" + 2*(player.streak-1) + " ";
                        else
                            messageRed += player.ToString() + " ";
                    }
                    textChannel.SendMessage(messageRed);

                    // End the game
                    endGame(textChannel, gameName);
                    break;
                case "draw":
                    // No stats recorded
                    textChannel.SendMessage("Game " + gameName + " has ended in a draw. No stats have been recorded.");

                    // End the game
                    endGame(textChannel, gameName);
                    break;
            }
        }

        /// <summary>
        /// Prints a list of all the games currently open/in progress.
        /// </summary>
        /// <param name="textChannel">Channel to send the message to.</param>
        public void getGames(Channel textChannel)
        {
            if (activeGame != null)
                textChannel.SendMessage("Game in lobby: " + activeGame.gameName + " (" + activeGame.waitingList.Count() + "/8)");
            else
                textChannel.SendMessage("No games in lobby.");

            if (runningGames.Count > 0)
            {
                String message = "Games in progress: ";
                foreach (var game in runningGames)
                {
                    message += game.gameName + " ";
                }
                textChannel.SendMessage(message);
            }
            else
                textChannel.SendMessage("No games in progress.");
        }

        /// <summary>
        /// Prints a users stats to the text channel.
        /// </summary>
        /// <param name="textChannel">Channel to send message to.</param>
        /// <param name="user">Users whos stats will be displayed.</param>
        public void getStats(Channel textChannel, User user, String message)
        {
            /*String[] splitMessage = message.Split(new char[] { ' ' });
            if (splitMessage.Count() == 2)
            {
                User otherUser = new User()
            }*/
            int wins = user.wins;
            int losses = user.losses;
            int gamesPlayed = wins + losses;
            textChannel.SendMessage(user.ToString() + " has " + gamesPlayed + " games played, " + wins + " wins, " + losses + " losses. \nCurrent win streak: " + user.streak + ".");
        }

        /// <summary>
        /// Broadcasts the top 5 players and their MMR to the text channel.
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to</param>
        /// <param name="ds">Database object</param>
        public void getTopMMR(Channel textChannel, DataStore ds)
        {
            // Sort dictionary by MMR
            var sortedDict = from entry in ds.users orderby entry.Value.mmr descending select entry;

            textChannel.SendMessage("Top 5 players by MMR:");
            string message = "";
            int i = 0;
            foreach (var obj in sortedDict)
            {
                if (i == 5)
                    break;
                message += "#" + (i + 1) + " " + obj.Value.ToString() + ", ";
                i++;
            }
            textChannel.SendMessage(message);
        }

        /// <summary>
        /// Broadcasts the top 5 players and their MMR to the text channel (sorted based on games played).
        /// </summary>
        /// <param name="textChannel">Channel to broadcast to</param>
        /// <param name="ds">Database object</param>
        public void getTopGames(Channel textChannel, DataStore ds)
        {
            // Sort dictionary by Games played
            var sortedDict = from entry in ds.users orderby entry.Value.mmr descending select entry;            // TODO: Add games played count to users, sort by games played.

            textChannel.SendMessage("Top 5 players by games played:");
            string message = "";
            int i = 0;
            foreach (var obj in sortedDict)
            {
                if (i == 5)
                    break;
                message += "#" + (i + 1) + " " + obj.Value.ToString() + ", ";
                i++;
            }
            textChannel.SendMessage(message);
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
            user.SendMessage(Resources.helpString);
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
        /// Extracts the game name from a "!xxxwins BBL#y" command
        /// </summary>
        /// <param name="message">Message sent by the user</param>
        /// <returns>String containing the game name.</returns>
        private String extractName (String message)
        {
            return message.Split(new char[] { ' ' }).Last();
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
    }
}
