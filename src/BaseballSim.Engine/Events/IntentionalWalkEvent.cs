using System.Collections.Generic;
using BaseballSim.Engine.Fielding;

namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 고의4구 (투구 없이 자동 출루)
    /// </summary>
    public sealed class IntentionalWalkEvent : GameEvent
    {
        public int PlateAppearanceNumber { get; set; }
        public int BatterId { get; set; }
        public int PitcherId { get; set; }
        public int OutsBefore { get; set; }
        public bool ByHuman { get; set; }
        public List<RunnerMovement> RunnerMovements { get; set; } = new List<RunnerMovement>();
        public int RunsScored { get; set; }
        public int AwayScoreAfter { get; set; }
        public int HomeScoreAfter { get; set; }
    }
}
