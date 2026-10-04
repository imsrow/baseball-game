using BaseballSim.Engine.Config;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 투구 결정에 주어지는 정보
    /// </summary>
    public sealed class PitchingContext
    {
        public GameState State { get; set; }

        public TeamSide Side { get; set; }

        public Player Pitcher { get; set; }

        public Player Batter { get; set; }

        /// <summary>타자의 실제 타석 방향</summary>
        public Hand BattingHand { get; set; }

        /// <summary>현재 투수 피로도 (0~1)</summary>
        public double Fatigue { get; set; }

        public LeagueConfig Config { get; set; }

        /// <summary>경기 난수원 (AI도 이것만 사용해야 재현성이 유지된다)</summary>
        public IRandomSource Random { get; set; }

        public int Balls => State.Balls;

        public int Strikes => State.Strikes;
    }
}
