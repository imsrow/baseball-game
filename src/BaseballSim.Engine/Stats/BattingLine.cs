using BaseballSim.Engine.Events;

namespace BaseballSim.Engine.Stats
{
    /// <summary>
    /// 타격 기록 (선수 개인 또는 리그 합계)
    /// </summary>
    public sealed class BattingLine
    {
        public int PlateAppearances { get; set; }
        public int AtBats { get; set; }
        public int Hits { get; set; }
        public int Doubles { get; set; }
        public int Triples { get; set; }
        public int HomeRuns { get; set; }
        public int Walks { get; set; }
        public int IntentionalWalks { get; set; }
        public int HitByPitch { get; set; }
        public int Strikeouts { get; set; }
        public int SacrificeFlies { get; set; }
        public int SacrificeBunts { get; set; }
        public int GroundedIntoDoublePlays { get; set; }
        public int ReachedOnErrors { get; set; }
        public int Runs { get; set; }
        public int RunsBattedIn { get; set; }

        public int Singles => Hits - Doubles - Triples - HomeRuns;

        public int TotalBases => Singles + 2 * Doubles + 3 * Triples + 4 * HomeRuns;

        public double Avg => Ratio(Hits, AtBats);

        public double Obp => Ratio(Hits + Walks + IntentionalWalks + HitByPitch,
            AtBats + Walks + IntentionalWalks + HitByPitch + SacrificeFlies);

        public double Slg => Ratio(TotalBases, AtBats);

        public double Ops => Obp + Slg;

        /// <summary>BABIP = (H − HR) / (AB − K − HR + SF)</summary>
        public double Babip => Ratio(Hits - HomeRuns, AtBats - Strikeouts - HomeRuns + SacrificeFlies);

        public double StrikeoutRate => Ratio(Strikeouts, PlateAppearances);

        /// <summary>고의4구 제외 볼넷 비율</summary>
        public double WalkRate => Ratio(Walks, PlateAppearances);

        public double HomeRunRate => Ratio(HomeRuns, PlateAppearances);

        public double HitByPitchRate => Ratio(HitByPitch, PlateAppearances);

        public void Record(PlateAppearanceOutcome outcome)
        {
            PlateAppearances++;
            if (PlateAppearanceOutcomeInfo.IsAtBat(outcome))
            {
                AtBats++;
            }

            switch (outcome)
            {
                case PlateAppearanceOutcome.Single: Hits++; break;
                case PlateAppearanceOutcome.Double: Hits++; Doubles++; break;
                case PlateAppearanceOutcome.Triple: Hits++; Triples++; break;
                case PlateAppearanceOutcome.HomeRun: Hits++; HomeRuns++; break;
                case PlateAppearanceOutcome.Walk: Walks++; break;
                case PlateAppearanceOutcome.IntentionalWalk: IntentionalWalks++; break;
                case PlateAppearanceOutcome.HitByPitch: HitByPitch++; break;
                case PlateAppearanceOutcome.Strikeout: Strikeouts++; break;
                case PlateAppearanceOutcome.SacrificeFly: SacrificeFlies++; break;
                case PlateAppearanceOutcome.SacrificeBunt: SacrificeBunts++; break;
                case PlateAppearanceOutcome.GroundedIntoDoublePlay: GroundedIntoDoublePlays++; break;
                case PlateAppearanceOutcome.ReachedOnError: ReachedOnErrors++; break;
            }
        }

        private static double Ratio(int numerator, int denominator)
        {
            return denominator == 0 ? 0 : (double)numerator / denominator;
        }
    }
}
