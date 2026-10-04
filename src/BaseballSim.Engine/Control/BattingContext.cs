using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 스윙 결정에 주어지는 정보.
    /// AI 타자는 Perceived(인지 위치)만 사용한다. Actual은 UI가 공을 그릴 때 쓰는 값이다.
    /// </summary>
    public sealed class BattingContext
    {
        public GameState State { get; set; }

        public TeamSide Side { get; set; }

        public Player Batter { get; set; }

        public Player Pitcher { get; set; }

        public Hand BattingHand { get; set; }

        /// <summary>같은 손 대결 (플래툰 불리)</summary>
        public bool SameHand { get; set; }

        public PitchType PitchType { get; set; }

        public double VelocityKmh { get; set; }

        /// <summary>실효 구위 z (속임수 정도)</summary>
        public double EffectiveStuffZ { get; set; }

        /// <summary>선구안 인지 오차가 적용된 위치</summary>
        public PerceivedPitch Perceived { get; set; }

        /// <summary>실제 투구 (UI 렌더링용, AI 판단에는 사용하지 않는다)</summary>
        public ExecutedPitch Actual { get; set; }

        public LeagueConfig Config { get; set; }

        public IRandomSource Random { get; set; }

        public int Balls => State.Balls;

        public int Strikes => State.Strikes;
    }
}
