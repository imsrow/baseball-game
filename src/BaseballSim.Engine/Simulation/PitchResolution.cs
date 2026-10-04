using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 투구 한 개의 판정 결과
    /// </summary>
    public sealed class PitchResolution
    {
        public PitchResult Result { get; set; }

        /// <summary>인플레이일 때 타구</summary>
        public BattedBall BattedBall { get; set; }

        /// <summary>인플레이일 때 수비·주루 결과</summary>
        public PlayResult Play { get; set; }
    }
}
