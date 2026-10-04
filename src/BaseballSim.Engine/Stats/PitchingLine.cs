using BaseballSim.Engine.Events;

namespace BaseballSim.Engine.Stats
{
    /// <summary>
    /// 투구 기록
    /// </summary>
    public sealed class PitchingLine
    {
        public int BattersFaced { get; set; }
        public int Outs { get; set; }
        public int Pitches { get; set; }
        public int Hits { get; set; }
        public int HomeRuns { get; set; }
        public int Walks { get; set; }
        public int HitByPitch { get; set; }
        public int Strikeouts { get; set; }
        public int Runs { get; set; }

        /// <summary>이닝 표기용 (아웃 / 3)</summary>
        public double Innings => Outs / 3.0;

        public double RunsPerNine => Outs == 0 ? 0 : Runs * 27.0 / Outs;

        public void Record(PlateAppearanceOutcome outcome)
        {
            BattersFaced++;
            if (PlateAppearanceOutcomeInfo.IsHit(outcome))
            {
                Hits++;
            }

            switch (outcome)
            {
                case PlateAppearanceOutcome.HomeRun: HomeRuns++; break;
                case PlateAppearanceOutcome.Walk:
                case PlateAppearanceOutcome.IntentionalWalk:
                    Walks++;
                    break;
                case PlateAppearanceOutcome.HitByPitch: HitByPitch++; break;
                case PlateAppearanceOutcome.Strikeout: Strikeouts++; break;
            }
        }
    }
}
