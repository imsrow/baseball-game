using BaseballSim.Engine.Events;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 투구 구역 하나의 집계. 타율·장타율·홈런율은 그 구역 공으로 끝난 타석 기준.
    /// </summary>
    public sealed class RegionStats
    {
        public int Pitches { get; private set; }
        public int Swings { get; private set; }
        public int Whiffs { get; private set; }
        public int InPlay { get; private set; }
        public int PlateAppearances { get; private set; }
        public int AtBats { get; private set; }
        public int Hits { get; private set; }
        public int TotalBases { get; private set; }
        public int HomeRuns { get; private set; }
        public int BattedBalls { get; private set; }
        public int SolidBalls { get; private set; }
        public double ExitVelocitySum { get; private set; }
        public double LaunchAngleSum { get; private set; }

        public void Add(PitchEvent ev)
        {
            Pitches++;
            if (ev.Swung && !ev.IsBunt)
            {
                Swings++;
                if (ev.Result == PitchResult.SwingingStrike)
                {
                    Whiffs++;
                }

                if (ev.Result == PitchResult.InPlay)
                {
                    InPlay++;
                    if (ev.BattedBall != null)
                    {
                        BattedBalls++;
                        ExitVelocitySum += ev.BattedBall.ExitVelocityKmh;
                        LaunchAngleSum += ev.BattedBall.LaunchAngleDeg;
                        if (ev.BattedBall.IsSolid)
                        {
                            SolidBalls++;
                        }
                    }
                }
            }

            if (!ev.PlateAppearanceOutcome.HasValue)
            {
                return;
            }

            PlateAppearances++;
            int bases = OutcomeRules.TotalBases(ev.PlateAppearanceOutcome.Value);
            if (OutcomeRules.IsAtBat(ev.PlateAppearanceOutcome.Value))
            {
                AtBats++;
            }

            if (bases > 0)
            {
                Hits++;
                TotalBases += bases;
            }

            if (ev.PlateAppearanceOutcome.Value == PlateAppearanceOutcome.HomeRun)
            {
                HomeRuns++;
            }
        }
    }
}
