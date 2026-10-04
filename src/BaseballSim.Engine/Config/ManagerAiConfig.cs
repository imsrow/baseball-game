namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 감독 AI 판단 기준
    /// </summary>
    public sealed class ManagerAiConfig
    {
        /// <summary>선발투수 교체 피로도 기준</summary>
        public double StarterFatigueThreshold { get; set; } = 0.55;

        /// <summary>구원투수 교체 피로도 기준</summary>
        public double RelieverFatigueThreshold { get; set; } = 0.35;

        /// <summary>선발투수 최대 투구 수 (피로도와 무관하게 교체)</summary>
        public int StarterMaxPitches { get; set; } = 115;

        /// <summary>구원투수가 한 번 등판해 상대하는 최대 타자 수</summary>
        public int RelieverMaxBattersFaced { get; set; } = 6;

        /// <summary>구원투수가 한 번 등판해 잡는 최대 아웃 수 (보통 1이닝)</summary>
        public int RelieverMaxOuts { get; set; } = 3;

        // ── 도루 ──

        /// <summary>예상 성공 확률이 이 값 이상일 때만 도루 시도</summary>
        public double StealMinSuccess { get; set; } = 0.72;

        /// <summary>조건을 만족할 때 투구 하나당 2루 도루 시도 확률</summary>
        public double StealAttemptPerPitch { get; set; } = 0.13;

        /// <summary>3루 도루 시도 확률 배율 (2루 대비)</summary>
        public double StealThirdAttemptFactor { get; set; } = 0.25;

        /// <summary>점수 차가 이 이상이면 도루하지 않음</summary>
        public int StealMaxRunDifference { get; set; } = 5;

        // ── 번트 ──

        /// <summary>희생번트 대상 타자: (컨택+파워)/2의 z가 이 값 미만</summary>
        public double SacrificeMaxBatterZ { get; set; } = -0.9;

        /// <summary>경기 후반 접전에서는 이 z 미만 타자도 희생번트</summary>
        public double LateCloseSacrificeMaxBatterZ { get; set; } = -0.3;

        /// <summary>경기 후반으로 보는 이닝</summary>
        public int LateInning { get; set; } = 7;

        /// <summary>접전으로 보는 점수 차</summary>
        public int CloseRunDifference { get; set; } = 1;

        /// <summary>희생번트 조건을 만족할 때 투구 하나당 사인 확률</summary>
        public double SacrificeSignPerPitch { get; set; } = 0.5;

        /// <summary>기습번트 대상: 스피드 이상</summary>
        public int BuntForHitMinSpeed { get; set; } = 65;

        /// <summary>기습번트 대상: 번트 능력치 이상</summary>
        public int BuntForHitMinBunt { get; set; } = 55;

        /// <summary>기습번트 조건을 만족할 때 초구 사인 확률</summary>
        public double BuntForHitSignRate { get; set; } = 0.04;

        // ── 고의4구 ──

        /// <summary>고의4구 대상 타자 타격 가치 z 이상</summary>
        public double IntentionalWalkMinBatterZ { get; set; } = 0.6;

        /// <summary>현재 타자와 다음 타자의 타격 가치 z 차이 이상</summary>
        public double IntentionalWalkMinGapZ { get; set; } = 0.7;

        // ── 대타·대주자·대수비 ──

        /// <summary>대타를 고려하는 점수 차 이내</summary>
        public int PinchHitMaxRunDifference { get; set; } = 2;

        /// <summary>벤치 타자가 현재 타자보다 타격 가치 z가 이만큼 높아야 대타</summary>
        public double PinchHitMinGapZ { get; set; } = 1.1;

        /// <summary>상대 투수와 반대 손 타자의 타격 가치 z 보너스 (같은 손은 감점)</summary>
        public double PlatoonValueZ { get; set; } = 0.3;

        /// <summary>대주자를 고려하는 이닝 이상</summary>
        public int PinchRunMinInning { get; set; } = 8;

        /// <summary>스피드가 이 값 이하인 주자만 교체</summary>
        public int PinchRunMaxRunnerSpeed { get; set; } = 40;

        /// <summary>대주자가 원래 주자보다 이만큼 빨라야 교체</summary>
        public int PinchRunMinSpeedGap { get; set; } = 20;

        /// <summary>앞선 경기에서 대수비를 고려하는 이닝 이상</summary>
        public int DefensiveSubMinInning { get; set; } = 8;

        /// <summary>대수비가 실효 수비력에서 이만큼 나아야 교체</summary>
        public double DefensiveSubMinGap { get; set; } = 18;

        /// <summary>실효 수비력이 이 값 미만인 포지션은 이닝 시작 시 보강 (대타 이후 등)</summary>
        public double DefensiveFixMinRating { get; set; } = 38;

        // ── 불펜 역할 ──

        /// <summary>세이브 상황 최대 리드</summary>
        public int SaveMaxLead { get; set; } = 3;

        /// <summary>마무리 투입 이닝 이상</summary>
        public int CloserInning { get; set; } = 9;

        /// <summary>셋업 투입 이닝</summary>
        public int SetupInning { get; set; } = 8;
    }
}
