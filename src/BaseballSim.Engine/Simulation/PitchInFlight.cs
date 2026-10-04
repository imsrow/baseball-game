using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Pitching;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 던져졌지만 아직 타자 판단 전인 투구
    /// </summary>
    public sealed class PitchInFlight
    {
        public PitchCall Call { get; set; }

        public ExecutedPitch Executed { get; set; }

        public PerceivedPitch Perceived { get; set; }

        public bool ByHuman { get; set; }
    }
}
