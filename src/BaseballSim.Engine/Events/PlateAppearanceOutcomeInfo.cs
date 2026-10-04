namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 타석 결과 분류 (기록 계산용)
    /// </summary>
    public static class PlateAppearanceOutcomeInfo
    {
        public static bool IsHit(PlateAppearanceOutcome outcome)
        {
            return outcome == PlateAppearanceOutcome.Single || outcome == PlateAppearanceOutcome.Double
                || outcome == PlateAppearanceOutcome.Triple || outcome == PlateAppearanceOutcome.HomeRun;
        }

        /// <summary>루타 수</summary>
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

        /// <summary>타수에 포함되는지 (볼넷·몸맞는공·희생타 제외)</summary>
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

        /// <summary>인플레이 타구로 끝난 타석인지 (홈런 포함)</summary>
        public static bool IsBallInPlay(PlateAppearanceOutcome outcome)
        {
            switch (outcome)
            {
                case PlateAppearanceOutcome.Walk:
                case PlateAppearanceOutcome.IntentionalWalk:
                case PlateAppearanceOutcome.HitByPitch:
                case PlateAppearanceOutcome.Strikeout:
                    return false;
                default:
                    return true;
            }
        }
    }
}
