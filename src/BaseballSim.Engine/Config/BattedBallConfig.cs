namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 인플레이 타구 생성 (타구속도, 발사각, 방향)
    /// 방향(spray): 0 = 중견수 방향, −45 = 3루선(좌측), +45 = 1루선(우측). 단위 도.
    /// </summary>
    public sealed class BattedBallConfig
    {
        // ── 정타 확률 ──

        /// <summary>타자 컨택 1 표준편차당 정타 로그 오즈 증가</summary>
        public double ContactSolidBeta { get; set; } = 0.30;

        /// <summary>투수 실효 구위 1 표준편차당 정타 로그 오즈 감소</summary>
        public double StuffSolidBeta { get; set; } = 0.22;

        // ── 타구속도 ──

        /// <summary>정타 타구속도 표준편차. 원본 7 mph</summary>
        public double SolidExitVelocitySdKmh { get; set; } = 11.3;

        /// <summary>빗맞은 타구 타구속도 표준편차. 원본 10.5 mph</summary>
        public double WeakExitVelocitySdKmh { get; set; } = 16.9;

        /// <summary>파워 1 표준편차당 정타 타구속도 증가. 원본 3 mph</summary>
        public double PowerExitVelocityKmhPerSd { get; set; } = 4.8;

        /// <summary>갭파워 1 표준편차당 정타 타구속도 증가. 원본 1 mph</summary>
        public double GapExitVelocityKmhPerSd { get; set; } = 1.6;

        /// <summary>파워 1 표준편차당 빗맞은 타구 타구속도 증가. 원본 1.2 mph</summary>
        public double WeakPowerExitVelocityKmhPerSd { get; set; } = 2.0;

        /// <summary>타구속도 하한. 원본 25 mph</summary>
        public double MinExitVelocityKmh { get; set; } = 40.0;

        /// <summary>타구속도 상한. 원본 121 mph</summary>
        public double MaxExitVelocityKmh { get; set; } = 195.0;

        // ── 발사각 ──

        /// <summary>정타 평균 발사각</summary>
        public double SolidLaunchAngleMeanDeg { get; set; } = 16.0;

        /// <summary>정타 발사각 표준편차</summary>
        public double SolidLaunchAngleSdDeg { get; set; } = 15.0;

        /// <summary>파워 1 표준편차당 정타 발사각 증가</summary>
        public double PowerLaunchAngleDegPerSd { get; set; } = 2.0;

        /// <summary>갭파워 1 표준편차당 정타 발사각 표준편차 로그 배율 감소 (라인드라이브 집중)</summary>
        public double GapLaunchAngleSdBeta { get; set; } = 0.08;

        /// <summary>최적 발사각에서 벗어날수록 정타 타구속도가 줄어드는 양 (편차 30도일 때)</summary>
        public double LaunchAnglePenaltyKmh { get; set; } = 14.0;

        /// <summary>타구속도가 가장 높게 나오는 발사각</summary>
        public double OptimalLaunchAngleDeg { get; set; } = 12.0;

        /// <summary>발사각 감속 기준 편차</summary>
        public double LaunchAnglePenaltyScaleDeg { get; set; } = 30.0;

        /// <summary>빗맞은 타구 중 공 윗부분을 친(땅볼성) 비율</summary>
        public double WeakToppedProbability { get; set; } = 0.58;

        public double ToppedLaunchAngleMeanDeg { get; set; } = -14.0;
        public double ToppedLaunchAngleSdDeg { get; set; } = 13.0;
        public double UnderLaunchAngleMeanDeg { get; set; } = 44.0;
        public double UnderLaunchAngleSdDeg { get; set; } = 12.0;

        /// <summary>투구 높이(존 중심 대비) 1 m당 발사각 변화</summary>
        public double LocationLaunchAngleDegPerM { get; set; } = 18.0;

        /// <summary>
        /// 투구 구역별 정타 로그 오즈 이동 (Heart, Shadow, Chase, Waste). LeagueConfig.LocationEffectScale을 곱해 적용.
        /// 인플레이 타구의 구역 분포로 가중 평균하면 0 근처가 되도록 잡아 리그 전체 수준은 유지한다
        /// </summary>
        public RegionTable SolidLogitShiftByRegion { get; set; } = new RegionTable(0.35, -0.55, -0.60, -0.85);

        /// <summary>투구 구역별 타구속도 이동 (km/h, Heart, Shadow, Chase, Waste). LocationEffectScale을 곱해 적용</summary>
        public RegionTable ExitVelocityKmhByRegion { get; set; } = new RegionTable(5.0, -6.0, -6.0, -9.0);

        /// <summary>구종별 발사각 보정 (FF, SI, FC, SL, CU, CH, FS)</summary>
        public PitchTypeTable PitchTypeLaunchAngleOffsetDeg { get; set; } = new PitchTypeTable(2.0, -5.0, 0.0, -1.0, -2.0, -3.0, -4.0);

        public double MinLaunchAngleDeg { get; set; } = -80.0;
        public double MaxLaunchAngleDeg { get; set; } = 85.0;

        // ── 방향 ──

        /// <summary>당겨치기 50 타자의 평균 당김 각도</summary>
        public double PullBaseDeg { get; set; } = 6.0;

        /// <summary>당겨치기 성향 1 표준편차당 당김 각도 증가</summary>
        public double PullDegPerSd { get; set; } = 5.0;

        /// <summary>몸쪽으로 1 m 들어온 투구일 때 추가 당김 각도</summary>
        public double InsidePullDegPerM { get; set; } = 40.0;

        /// <summary>땅볼(발사각 10도 미만) 추가 당김 각도</summary>
        public double GroundBallExtraPullDeg { get; set; } = 5.0;

        /// <summary>땅볼 추가 당김을 적용하는 발사각 상한</summary>
        public double GroundBallPullMaxLaunchAngleDeg { get; set; } = 10.0;

        /// <summary>방향 표준편차</summary>
        public double SpraySdDeg { get; set; } = 22.0;

        /// <summary>페어 지역 반각 (파울라인)</summary>
        public double FairAngleDeg { get; set; } = 45.0;
    }
}
