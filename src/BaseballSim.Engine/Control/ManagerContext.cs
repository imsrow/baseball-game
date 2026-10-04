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

        /// <summary>
        /// 사람 감독에게 확인을 요청할 때 AI가 추천하는 지시 (입력 대기 중 UI 표시용, 없으면 null)
        /// </summary>
        public ManagerOrders Suggestion { get; set; }

        public TeamGameState Team => State.Team(Side);

        public TeamGameState Opponent => State.Team(Side == TeamSide.Away ? TeamSide.Home : TeamSide.Away);
    }
}
