using System.Collections.Generic;
using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Teams
{
    /// <summary>
    /// 팀 (가상 구단)
    /// </summary>
    public sealed class Team
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Player> Roster { get; set; } = new List<Player>();

        public Player FindPlayer(int playerId)
        {
            for (int i = 0; i < Roster.Count; i++)
            {
                if (Roster[i].Id == playerId)
                {
                    return Roster[i];
                }
            }

            return null;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
