namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 투구 실행(구속, 제구)과 AI 투수의 구종·로케이션 선택
    /// </summary>
    public sealed class PitchConfig
    {
        // ── 구속 ──

        /// <summary>구속 50 투수의 포심 평균 구속. 원본 94.2 mph (MLB 2023)</summary>
        public double FastballBaseVelocityKmh { get; set; } = 151.6;

        /// <summary>구속 능력치 1 표준편차당 구속 변화. 원본 2.4 mph</summary>
        public double VelocityKmhPerSd { get; set; } = 3.9;

        /// <summary>투구마다 구속 흔들림 (표준편차). 원본 0.8 mph</summary>
        public double VelocityNoiseKmh { get; set; } = 1.3;

        /// <summary>
        /// 포심 대비 구종별 구속 차이 (FF, SI, FC, SL, CU, CH, FS)
        /// 원본 mph: 0, −0.8, −4.7, −8.6, −13.1, −8.3, −7.7
        /// </summary>
        public PitchTypeTable VelocityOffsetKmh { get; set; } = new PitchTypeTable(0.0, -1.3, -7.6, -13.8, -21.1, -13.4, -12.4);

        // ── 제구 ──

        /// <summary>제구 50 투수의 목표 대비 위치 오차 표준편차 (축별, m)</summary>
        public double ControlSigmaM { get; set; } = 0.12;

        /// <summary>제구 1 표준편차당 오차의 로그 배율 감소</summary>
        public double ControlBeta { get; set; } = 0.18;

        // ── 구위 ──

        /// <summary>빠른 공 계열에서 구속 z가 실효 구위 z에 더해지는 비율</summary>
        public double FastballVelocityStuffWeight { get; set; } = 0.5;

        /// <summary>변화구·오프스피드에서 구속 z가 실효 구위 z에 더해지는 비율</summary>
        public double OffspeedVelocityStuffWeight { get; set; } = 0.15;

        // ── AI 구종 선택 ──

        /// <summary>카운트별 빠른 공 계열 가중치 배율 (변화구는 1)</summary>
        public CountTable FastballWeightByCount { get; set; } = new CountTable(
            1.00, 0.85, 0.70,
            1.20, 0.95, 0.75,
            1.50, 1.15, 0.85,
            2.50, 1.60, 1.10);

        /// <summary>구위 1 표준편차당 구사 가중치 로그 배율 증가</summary>
        public double StuffUsageBeta { get; set; } = 0.15;

        // ── AI 로케이션 선택 ──

        /// <summary>존 안 목표: 존 반폭·반높이 대비 목표 분포 범위</summary>
        public double InZoneTargetSpread { get; set; } = 0.75;

        /// <summary>빠른 공 목표 높이 치우침 (존 반높이 대비, +가 높은 쪽)</summary>
        public double FastballVerticalBias { get; set; } = 0.25;

        /// <summary>브레이킹볼 목표 높이 치우침</summary>
        public double BreakingVerticalBias { get; set; } = -0.35;

        /// <summary>오프스피드 목표 높이 치우침</summary>
        public double OffspeedVerticalBias { get; set; } = -0.40;

        /// <summary>존 밖 목표: 존 경계 바깥 최소 거리 (m)</summary>
        public double ChaseTargetMinM { get; set; } = 0.04;

        /// <summary>존 밖 목표: 존 경계 바깥 최대 거리 (m)</summary>
        public double ChaseTargetMaxM { get; set; } = 0.22;

        /// <summary>빠른 공으로 존 밖을 노릴 때 높은 쪽을 고를 확률 (나머지는 바깥쪽)</summary>
        public double FastballChaseUpProbability { get; set; } = 0.55;

        /// <summary>변화구로 존 밖을 노릴 때 낮은 쪽을 고를 확률 (나머지는 바깥쪽)</summary>
        public double BreakingChaseDownProbability { get; set; } = 0.70;
    }
}
