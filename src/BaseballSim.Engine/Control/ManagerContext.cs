using BaseballSim.Engine.Config;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.State;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 감독 결정에 주어지는 정보
    /// </summary>
    public sealed class ManagerContext
    {
        public GameState State { get; set; }

        /// <summary>결정하는 팀</summary>
        public TeamSide Side { get; set; }

        public PlayerDirectory Players { get; set; }

        public LeagueConfig Config { get; set; }

        public IRandomSource Random { get; set; }

        public TeamGameState Team => State.Team(Side);

        public TeamGameState Opponent => State.Team(Side == TeamSide.Away ? TeamSide.Home : TeamSide.Away);
    }
}
