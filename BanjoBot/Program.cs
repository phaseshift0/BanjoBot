using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
    
namespace BanjoBot
{
    class Program
    {
        public static DataStore ds;
        private static Commands cmd = new Commands();
        private static DiscordClient bot;

        static void Main(string[] args)
        {
            bot = new DiscordClient();

            // Bots login credentials: username///pass
            //bot.Connect("banjobotdiscord@gmail.com", "banjobotliverpool");              // Real bot
            bot.Connect("banjotestbot@gmail.com", "banjotestbot1");                    // Test bot

            // Set up the DataStore
            ds = new DataStore();

            Console.Out.WriteLine("BanjoBot online.");

            // Runs whenever a message is received in chat
            bot.MessageReceived += Bot_MessageReceived;

            bot.Wait();
        }

        /// <summary>
        /// Interprets commands written in chat and calls appropriate Command function.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Bot_MessageReceived(object sender, MessageEventArgs e)
        {
            Discord.User asdas = e.User;

            // If message does not start with '!', ignore it.
            if (e.Message.RawText.StartsWith("!"))
            {
                Channel textChannel = e.Channel;
                
                // Get or create user (should be moved to DataStore.getUser())
                User user = ds.getUser(e.User.Id, e.User.Name);

                String command = e.Message.RawText.Split(new char[] { ' ' }).First();
                switch (command)
                {
                    case "!hostgame":
                        cmd.hostGame(textChannel, user);
                        break;
                    case "!join":
                        cmd.joinGame(textChannel, user);
                        break;
                    case "!leave":
                        cmd.leaveGame(textChannel, user);
                        break;
                    case "!startgame":
                        cmd.startGame(textChannel, user);
                        break;
                    case "!cancel":
                        cmd.cancelGame(textChannel, user);
                        break;
                    case "!list":
                        cmd.listPlayers(textChannel);
                        break;
                    case "!bluewins":
                        cmd.voteWinner(e, user, Teams.Blue);
                        break;
                    case "!redwins":
                        cmd.voteWinner(e, user, Teams.Red);
                        break;
                    case "!draw":
                        cmd.voteWinner(e, user, Teams.Draw);
                        break;
                    case "!getgames":
                        cmd.getGames(textChannel);
                        break;
                    case "!stats":
                        cmd.getStats(textChannel, user, e.Message.RawText);
                        break;
                    case "!top":
                        cmd.getTopMMR(textChannel, ds);
                        break;
                    case "!save":
                        cmd.saveData(user.id);
                        break;
                    case "!help":
                        cmd.printHelp(e.User);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
