using BaseballSim.Engine.Events;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 타석 결과 분류 (타수 여부, 루타 수)
    /// </summary>
    public static class OutcomeRules
    {
        public static bool IsAtBat(PlateAppearanceOutcome outcome)
        {
            switch (outcome)
            {
                case PlateAppearanceOutcome.Walk:
                case PlateAppearanceOutcome.IntentionalWalk:
                case PlateAppearanceOutcome.HitByPitch:
                case PlateAppearanceOutcome.SacrificeFly:
                case PlateAppearanceOutcome.SacrificeBunt:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>안타면 루타 수, 아니면 0</summary>
        public static int TotalBases(PlateAppearanceOutcome outcome)
        {
            switch (outcome)
            {
                case PlateAppearanceOutcome.Single: return 1;
                case PlateAppearanceOutcome.Double: return 2;
                case PlateAppearanceOutcome.Triple: return 3;
                case PlateAppearanceOutcome.HomeRun: return 4;
                default: return 0;
            }
        }

        public static bool IsWalk(PlateAppearanceOutcome outcome)
        {
            return outcome == PlateAppearanceOutcome.Walk || outcome == PlateAppearanceOutcome.IntentionalWalk;
        }
    }
}
