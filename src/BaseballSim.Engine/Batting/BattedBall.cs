namespace BaseballSim.Engine.Batting
{
    /// <summary>
    /// 생성된 인플레이 타구의 초기 조건
    /// </summary>
    public sealed class BattedBall
    {
        public double ExitVelocityKmh { get; set; }

        public double LaunchAngleDeg { get; set; }

        /// <summary>방향각: 0 중견수, −45 3루선, +45 1루선</summary>
        public double SprayAngleDeg { get; set; }

        /// <summary>정타 여부</summary>
        public bool IsSolid { get; set; }

        public BattedBallType Type => BattedBallClassifier.Classify(LaunchAngleDeg);
    }
}
