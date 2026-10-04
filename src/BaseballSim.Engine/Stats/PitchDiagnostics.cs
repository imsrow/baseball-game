using BaseballSim.Engine.Batting;

namespace BaseballSim.Engine.Stats
{
    /// <summary>
    /// 투구·타구 단위 진단 지표 (튜닝 판단용)
    /// </summary>
    public sealed class PitchDiagnostics
    {
        public long Pitches { get; set; }
        public long InZone { get; set; }
        public long Swings { get; set; }
        public long ZoneSwings { get; set; }
        public long ChaseSwings { get; set; }
        public long Contacts { get; set; }
        public long ZoneContacts { get; set; }
        public long ChaseContacts { get; set; }
        public long Fouls { get; set; }
        public long InPlay { get; set; }
        public long CalledStrikes { get; set; }
        public long[] BattedBallTypes { get; set; } = new long[4];
        /// <summary>타구 유형별 인플레이 타구 수 (홈런 제외)</summary>
        public long[] BallsInPlayByType { get; set; } = new long[4];

        /// <summary>타구 유형별 안타 수 (홈런 제외)</summary>
        public long[] HitsByType { get; set; } = new long[4];

        public double ExitVelocitySumKmh { get; set; }
        public double LaunchAngleSumDeg { get; set; }
        public long Errors { get; set; }
        public long Runs { get; set; }

        public double ZoneRate => Ratio(InZone, Pitches);
        public double SwingRate => Ratio(Swings, Pitches);
        public double ZoneSwingRate => Ratio(ZoneSwings, InZone);
        public double ChaseRate => Ratio(ChaseSwings, Pitches - InZone);
        public double ContactRate => Ratio(Contacts, Swings);
        public double ZoneContactRate => Ratio(ZoneContacts, ZoneSwings);
        public double ChaseContactRate => Ratio(ChaseContacts, ChaseSwings);
        public double WhiffPerSwing => 1.0 - ContactRate;
        public double FoulRate => Ratio(Fouls, Pitches);
        public double InPlayRate => Ratio(InPlay, Pitches);
        public double CalledStrikeRate => Ratio(CalledStrikes, Pitches);
        public double AverageExitVelocityKmh => Ratio(ExitVelocitySumKmh, InPlay);
        public double AverageLaunchAngleDeg => Ratio(LaunchAngleSumDeg, InPlay);

        public double BattedBallTypeRate(BattedBallType type)
        {
            return Ratio(BattedBallTypes[(int)type], InPlay);
        }

        /// <summary>타구 유형별 BABIP (홈런 제외 안타 / 홈런 제외 인플레이)</summary>
        public double BabipByType(BattedBallType type)
        {
            return Ratio(HitsByType[(int)type], BallsInPlayByType[(int)type]);
        }

        private static double Ratio(double numerator, double denominator)
        {
            return denominator == 0 ? 0 : numerator / denominator;
        }
    }
}
