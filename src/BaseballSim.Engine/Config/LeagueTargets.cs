namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 리그 목표 지표와 허용 오차 (하네스 판정용), 참고치 (판정 없이 비교만)
    /// 비율 지표는 모두 타석(PA) 대비. BABIP = (H − HR) / (AB − K − HR + SF)
    /// </summary>
    public sealed class LeagueTargets
    {
        public double Avg { get; set; } = 0.245;
        public double Obp { get; set; } = 0.315;
        public double Slg { get; set; } = 0.400;
        public double StrikeoutRate { get; set; } = 0.22;
        public double WalkRate { get; set; } = 0.085;
        public double HomeRunRate { get; set; } = 0.030;
        public double Babip { get; set; } = 0.290;

        public double AvgTolerance { get; set; } = 0.005;
        public double ObpTolerance { get; set; } = 0.005;
        public double SlgTolerance { get; set; } = 0.010;
        public double StrikeoutRateTolerance { get; set; } = 0.005;
        public double WalkRateTolerance { get; set; } = 0.005;
        public double HomeRunRateTolerance { get; set; } = 0.003;
        public double BabipTolerance { get; set; } = 0.005;

        // ── 참고치 (MLB 2023 근사, 팀당 경기당) ──
        public double ReferenceStealAttemptsPerTeamGame { get; set; } = 0.9;
        public double ReferenceStealSuccessRate { get; set; } = 0.78;
        public double ReferenceSacrificeBuntsPerTeamGame { get; set; } = 0.08;
        public double ReferenceIntentionalWalksPerTeamGame { get; set; } = 0.08;
        public double ReferenceWildPitchPassedBallPerTeamGame { get; set; } = 0.4;
        public double ReferenceErrorsPerTeamGame { get; set; } = 0.55;
        public double ReferencePitchesPerPlateAppearance { get; set; } = 3.9;
    }
}
