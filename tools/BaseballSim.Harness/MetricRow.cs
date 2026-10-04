namespace BaseballSim.Harness
{
    /// <summary>
    /// 보고서 한 줄: 시드별 값의 평균·표준편차와 목표 비교
    /// </summary>
    public sealed class MetricRow
    {
        public string Name { get; set; }

        public double Mean { get; set; }

        public double StandardDeviation { get; set; }

        public double? Target { get; set; }

        public double? Tolerance { get; set; }

        /// <summary>true면 .xxx 형식, false면 % 형식</summary>
        public bool IsRateStat { get; set; }

        public bool Passed => !Target.HasValue || !Tolerance.HasValue
            || System.Math.Abs(Mean - Target.Value) <= Tolerance.Value + 1e-12;
    }
}
