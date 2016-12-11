using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBot {
    class League {
        private Game _activeGame;
        private List<Game> _runningGames;
        public Channel Channel { get; set; }
        public Role Role { get; set; }
        public String Name { get; set; }

        public League(String name, Channel channel) {
            _runningGames = new List<Game>();
            Channel = channel;
            Name = name;
        }

        public League(String name, Channel channel, Role role) : this(name, channel)
        {
            Role = role;
        }

        public Game HostGame(Player host)
        {
            Game game = new Game(host, DataStore.GetInstance().getGameCounter());
            _activeGame = game;

            return game;
        }

        public Game GetActiveGame()
        {
            return _activeGame;
        }

        public void CloseGame(Game game, Teams winnerTeam)
        {
            game.AdjustStats(winnerTeam);
            _runningGames.Remove(game);
            foreach (Player player in game.WaitingList)
            {
                player.CurrentGame = null;
            }

            saveData();
        }

        private void saveData()
        {
                        //TODO: Saving to different xmls or mysql server
        }

        public void StartGame()
        {
            foreach(Player player in _activeGame.WaitingList)
            {
                player.CurrentGame = _activeGame;
            }
            _activeGame.StartGame();
            _runningGames.Add(_activeGame);
            _activeGame = null;
        }

        public void CancelGame()
        {
            _activeGame = null;
        }

        public bool LobbyExists()
        {
            return _activeGame != null;
        }

        public int GetAndIncGameCounter()
        {
            throw new NotImplementedException();
        }

        public List<Game> GetRunningGames()
        {
            return _runningGames;
        }
    }
}
