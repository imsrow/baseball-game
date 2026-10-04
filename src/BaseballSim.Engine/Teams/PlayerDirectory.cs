using System.Collections.Generic;
using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Teams
{
    /// <summary>
    /// 경기에 참가하는 선수 ID → 선수 조회
    /// </summary>
    public sealed class PlayerDirectory
    {
        private readonly Dictionary<int, Player> _players = new Dictionary<int, Player>();

        public void Add(Player player)
        {
            _players[player.Id] = player;
        }

        public Player Get(int playerId)
        {
            return _players[playerId];
        }

        public bool TryGet(int playerId, out Player player)
        {
            return _players.TryGetValue(playerId, out player);
        }

        public IEnumerable<Player> All => _players.Values;

        public static PlayerDirectory FromSetup(GameSetup setup)
        {
            var directory = new PlayerDirectory();
            foreach (Player p in setup.Away.Team.Roster)
            {
                directory.Add(p);
            }

            foreach (Player p in setup.Home.Team.Roster)
            {
                directory.Add(p);
            }

            return directory;
        }
    }
}
