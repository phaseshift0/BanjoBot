using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BanjoBot
{
    class DataStore
    {
        private static DataStore singleton;
        //Props
        public static int gameCounter;
        public Dictionary<ulong, Player> users;
        public Dictionary<String, Game> games;

        /// <summary>
        /// Constructor
        /// </summary>
        private DataStore()
        {
            users = new Dictionary<ulong, Player>();
            games = new Dictionary<string, Game>();

            if (File.Exists(@"lib\BanjoStore.xml"))
                readXML();
            else
                gameCounter = 1;
        }

        public static DataStore GetInstance()
        {
            if (singleton != null)
            {
                return singleton;
            }
            else
            {
                singleton = new DataStore();
                return singleton;
            }
        }
       
        /// <summary>
        /// Gets the user from the DataStore or creates a new one and adds it to the DataStore.
        /// </summary>
        /// <param _name="id">User's ID</param>
        /// <param _name="name">User's _name</param>
        /// <returns>The User.</returns>
        public Player getPlayer(ulong id, String name, String mention, User user)
        {
            // If user exists in DataStore
            if (users.ContainsKey(id))
            {
                // Used to upgrade data in BanjoStore from v1.2
                if (users[id].Mention == "")
                    users[id].Mention = mention;
                return users[id];
            }


            Player player = new Player(id, name, mention, user);
            users.Add(player.ID, player);

            return player;
        }

        /// <summary>
        /// Gets the user from the DataStore. Returns null if _user does not exist.
        /// </summary>
        /// <param _name="id">User's ID.</param>
        /// <returns>The User if it exists, null otherwise.</returns>
        public Player getPlayer(ulong id)
        {
            // If user exists in DataStore
            if (users.ContainsKey(id))
                return users[id];

            return null;
        }

        /// <summary>
        /// Gets and increments gameCounter
        /// </summary>
        /// <returns>gameCounter</returns>
        public int getGameCounter()
        {
            return gameCounter++;
        }

        /// <summary>
        /// Writes gameCounter and Users to xml file.
        /// </summary>
        public void writeXML()
        {
            // Prepare XML writer settings
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };

            // Write data to file.
            using (XmlWriter writer = XmlWriter.Create(@"lib\BanjoStore.xml", settings))
            {
                writer.WriteStartDocument();

                // Root
                writer.WriteStartElement("DataStore");

                // Game Counter Element
                writer.WriteStartElement("GameCounter");
                writer.WriteElementString("Number", gameCounter.ToString());
                writer.WriteFullEndElement();

                // Users Element
                writer.WriteStartElement("Users");
                foreach (var obj in users)
                {
                    // User Element
                    writer.WriteStartElement("User");
                    writer.WriteElementString("ID", obj.Value.ID.ToString());
                    writer.WriteElementString("Name", obj.Value.Name);
                    writer.WriteElementString("Mention", obj.Value.Mention);
                    writer.WriteElementString("MMR", obj.Value.Mmr.ToString());
                    writer.WriteElementString("Wins", obj.Value.Wins.ToString());
                    writer.WriteElementString("Losses", obj.Value.Losses.ToString());
                    writer.WriteElementString("Streak", obj.Value.Streak.ToString());
                    writer.WriteFullEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Reads data from xml file and sets gameCounter and creates Users.
        /// </summary>
        public void readXML()
        {
            // Load the XML document
            XmlDocument document = new XmlDocument();
            document.Load(@"lib\BanjoStore.xml");

            // Retrieve and set the gameCounter
            XmlNodeList nodes = document.GetElementsByTagName("GameCounter");
            foreach (XmlNode node in nodes)
            {
                 gameCounter = Convert.ToInt32(node.InnerText);
            }
                
            // Retrive Users and add to Dictionary
            nodes = document.GetElementsByTagName("User");
            foreach (XmlNode node in nodes)
            {
                Player user;
                if (node.ChildNodes.Count == 7)
                {
                    user = new Player(
                        Convert.ToUInt64(node.ChildNodes[0].InnerText),
                        node.ChildNodes[1].InnerText,
                        node.ChildNodes[2].InnerText,
                        Convert.ToInt32(node.ChildNodes[3].InnerText),
                        Convert.ToInt32(node.ChildNodes[4].InnerText),
                        Convert.ToInt32(node.ChildNodes[5].InnerText),
                        Convert.ToInt32(node.ChildNodes[6].InnerText)
                        );
                }
                // for upgrading existing XML file to be compatible with BanjoBot v1.2
                else
                {
                    user = new Player(
                        Convert.ToUInt64(node.ChildNodes[0].InnerText),
                        node.ChildNodes[1].InnerText,
                        Convert.ToInt32(node.ChildNodes[2].InnerText),
                        Convert.ToInt32(node.ChildNodes[3].InnerText),
                        Convert.ToInt32(node.ChildNodes[4].InnerText),
                        Convert.ToInt32(node.ChildNodes[5].InnerText)
                        );
                }

                users.Add(user.ID, user);
            }
        }
    }
}
