namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 주루: 주자 시간, 추가 진루 판단, 태그업
    /// </summary>
    public sealed class BaserunningConfig
    {
        /// <summary>스피드 50 우타자의 홈→1루 시간</summary>
        public double HomeToFirstS { get; set; } = 4.35;

        /// <summary>좌타자 홈→1루 단축</summary>
        public double LeftyHomeToFirstBonusS { get; set; } = 0.08;

        /// <summary>스피드 1 표준편차당 홈→1루 단축</summary>
        public double HomeToFirstPerSd { get; set; } = 0.15;

        /// <summary>스피드 50 주자의 베이스 간 시간 (리드 포함, 타구 순간 출발)</summary>
        public double BaseToBaseS { get; set; } = 3.70;

        /// <summary>스피드 1 표준편차당 베이스 간 단축</summary>
        public double BaseToBasePerSd { get; set; } = 0.13;

        /// <summary>첫 베이스 이후 추가 베이스 시간 (이미 전력 질주 중이라 더 빠르다)</summary>
        public double ExtraBaseS { get; set; } = 3.40;

        /// <summary>스피드 1 표준편차당 추가 베이스 시간 단축</summary>
        public double ExtraBasePerSd { get; set; } = 0.12;

        /// <summary>2아웃 미만 뜬공·라인드라이브에서 주자가 타구를 확인하느라 늦는 시간</summary>
        public double FlyBallHoldDelayS { get; set; } = 1.0;

        /// <summary>추가 진루 시도에 필요한 예상 여유 시간</summary>
        public double AdvanceMarginS { get; set; } = 0.15;

        /// <summary>주루 센스 1 표준편차당 필요 여유 시간 감소 (더 정확한 판단으로 공격적)</summary>
        public double InstinctMarginPerSdS { get; set; } = 0.05;

        /// <summary>주루 센스 50 주자의 여유 시간 추정 오차 (표준편차)</summary>
        public double EstimateNoiseS { get; set; } = 0.35;

        /// <summary>주루 센스 1 표준편차당 추정 오차 로그 배율 감소</summary>
        public double InstinctNoiseBeta { get; set; } = 0.25;

        /// <summary>태그업 출발 반응 시간</summary>
        public double TagUpReactionS { get; set; } = 0.0;

        /// <summary>2아웃 미만 내야 땅볼 아웃(1루 송구) 때 3루 주자가 홈에 들어올 확률</summary>
        public double ThirdScoresOnGroundOutProbability { get; set; } = 0.55;

        /// <summary>내야 땅볼 아웃(1루 송구) 때 비포스 2루 주자가 3루로 갈 확률</summary>
        public double SecondAdvancesOnGroundOutProbability { get; set; } = 0.50;
    }
}
