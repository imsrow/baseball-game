namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 수비: 반응, 이동, 포구, 송구, 땅볼 진행
    /// </summary>
    public sealed class FieldingConfig
    {
        // ── 반응·이동 ──

        public double InfieldReactionS { get; set; } = 0.30;
        public double OutfieldReactionS { get; set; } = 1.00;

        /// <summary>외야수는 라인드라이브 궤적 판단이 어려워 첫 반응이 더 늦다</summary>
        public double OutfieldLineDriveExtraReactionS { get; set; } = 0.35;

        /// <summary>투수는 투구 동작을 마무리하느라 추가로 늦는다</summary>
        public double PitcherExtraReactionS { get; set; } = 0.25;

        /// <summary>포수는 마스크를 벗고 일어나느라 추가로 늦는다</summary>
        public double CatcherExtraReactionS { get; set; } = 0.30;

        /// <summary>수비범위 1 표준편차당 반응 시간 단축</summary>
        public double ReactionSPerSd { get; set; } = 0.04;

        /// <summary>내야수 횡이동 평균 속도 (가속 포함, m/s)</summary>
        public double InfieldSpeedMps { get; set; } = 4.2;

        /// <summary>
        /// 내야수가 뜬공을 쫓아 달릴 때 평균 속도 (m/s). 땅볼 횡이동(InfieldSpeedMps)보다 빠르다.
        /// 내야와 외야 사이에 뜬 짧은 뜬공 처리
        /// </summary>
        public double InfieldAirBallSpeedMps { get; set; } = 6.5;

        /// <summary>외야수 평균 이동 속도 (가속 포함, m/s). 최고 속도 원본 27 ft/s(8.2 m/s)보다 낮게 둔다</summary>
        public double OutfieldSpeedMps { get; set; } = 7.2;

        /// <summary>내야수 수비범위 1 표준편차당 이동 속도 증가</summary>
        public double InfieldSpeedMpsPerSd { get; set; } = 0.17;

        /// <summary>외야수 수비범위 1 표준편차당 이동 속도 증가</summary>
        public double OutfieldSpeedMpsPerSd { get; set; } = 0.35;

        /// <summary>제자리에서 손이 닿는 거리 (팔·다이빙 포함)</summary>
        public double InfieldReachM { get; set; } = 1.5;
        public double OutfieldReachM { get; set; } = 1.4;

        /// <summary>펜스 앞 포구: 펜스 위치 확인·점프 때문에 더 걸리는 시간</summary>
        public double WallCatchExtraS { get; set; } = 0.50;

        /// <summary>뜬공 포구 확률 = logistic(시간 여유 / 이 값)</summary>
        public double CatchProbabilityScaleS { get; set; } = 0.25;

        // ── 실책 ──

        public double AirBallErrorRate { get; set; } = 0.008;
        public double GroundBallErrorRate { get; set; } = 0.016;
        public double ThrowErrorRate { get; set; } = 0.010;

        /// <summary>포구 1 표준편차당 포구 실책 로그 오즈 감소</summary>
        public double HandsErrorBeta { get; set; } = 0.35;

        /// <summary>송구 정확도 1 표준편차당 송구 실책 로그 오즈 감소</summary>
        public double AccuracyErrorBeta { get; set; } = 0.35;

        /// <summary>어려운 포구(여유 시간 부족)일 때 포구 실책 로그 오즈 증가 (난이도 1일 때)</summary>
        public double DifficultyErrorShift { get; set; } = 1.2;

        /// <summary>송구 거리 1 m당 송구 실책 로그 오즈 증가 (기준 거리 대비)</summary>
        public double ThrowErrorPerMeter { get; set; } = 0.015;

        /// <summary>ThrowErrorRate가 적용되는 기준 송구 거리</summary>
        public double ThrowErrorReferenceDistanceM { get; set; } = 35.0;

        // ── 송구 ──

        public double InfieldTransferS { get; set; } = 0.85;
        public double OutfieldTransferS { get; set; } = 1.00;

        /// <summary>어려운 포구 후 추가 송구 준비 시간 (난이도 1일 때)</summary>
        public double DifficultyTransferExtraS { get; set; } = 0.40;

        /// <summary>내야 송구 속도. 원본 약 78 mph</summary>
        public double InfieldThrowSpeedMps { get; set; } = 35.0;

        /// <summary>외야 송구 유효 속도 (포물선·원바운드 포함 평균 수평 속도). 원본 약 65 mph</summary>
        public double OutfieldThrowSpeedMps { get; set; } = 29.0;

        /// <summary>송구 강도 1 표준편차당 송구 속도 증가</summary>
        public double ThrowSpeedMpsPerSd { get; set; } = 2.0;

        /// <summary>병살 피벗 동작 시간</summary>
        public double PivotS { get; set; } = 0.80;

        /// <summary>선행 주자 포스아웃을 노리기 위한 최소 예상 여유 시간</summary>
        public double LeadForceMarginS { get; set; } = 0.25;

        /// <summary>송구 도착 시간 흔들림 (표준편차)</summary>
        public double PlayTimingNoiseS { get; set; } = 0.15;

        // ── 땅볼 진행 ──

        /// <summary>땅볼 판정 발사각 상한 (이 각도 미만은 땅볼 모델)</summary>
        public double GroundBallMaxLaunchAngleDeg { get; set; } = 10.0;

        /// <summary>첫 바운드 후 수평 속도 유지 비율</summary>
        public double GroundBallSpeedRetention { get; set; } = 0.90;

        /// <summary>발사각이 음수일 때 1도당 추가 속도 손실 (내리찍힌 타구일수록 첫 바운드에서 많이 죽는다)</summary>
        public double GroundBallRetentionLossPerDeg { get; set; } = 0.02;

        /// <summary>첫 바운드 후 속도 유지 비율 하한</summary>
        public double GroundBallMinRetention { get; set; } = 0.25;

        /// <summary>내야 땅볼 감속 (m/s², 구름 마찰)</summary>
        public double GroundBallDecelerationMps2 { get; set; } = 4.0;

        /// <summary>내야수가 막을 수 있는 최대 거리 (이보다 멀면 외야로 빠짐)</summary>
        public double InfieldInterceptLimitM { get; set; } = 47.0;

        /// <summary>땅볼 경로 탐색 간격</summary>
        public double InterceptScanStepM { get; set; } = 0.25;

        /// <summary>
        /// 땅볼 처리 난이도 기준: 야수 도착이 공보다 이 시간 이내로 빠르면 어려운 타구
        /// </summary>
        public double DifficultPlayWindowS { get; set; } = 0.30;

        // ── 외야 타구 회수 ──

        /// <summary>외야수가 굴러가는 공 앞에서 속도를 줄이고 공을 집어 드는 시간</summary>
        public double OutfieldPickupS { get; set; } = 0.85;

        /// <summary>
        /// 멀어지는 공을 쫓아가 잡을 때 추가 시간 (멈추고 돌아서 송구 자세). 정면으로 달려 나오면 0,
        /// 옆으로 끊으면 절반, 뒤에서 쫓아가면 전부
        /// </summary>
        public double OutfieldChasePickupExtraS { get; set; } = 1.10;

        /// <summary>외야 잔디에 떨어진 공의 첫 바운드 후 수평 속도 유지 비율</summary>
        public double OutfieldLandingSpeedRetention { get; set; } = 0.75;

        /// <summary>외야 잔디 구름 감속 (m/s²)</summary>
        public double OutfieldRollDecelerationMps2 { get; set; } = 3.5;

        /// <summary>펜스까지 굴러가거나 맞은 공을 처리하는 추가 지연 (최소)</summary>
        public double WallCaromDelayS { get; set; } = 0.6;

        /// <summary>펜스 플레이 추가 지연의 무작위 폭 (0 ~ 이 값, 튀는 방향에 따라)</summary>
        public double WallCaromRandomS { get; set; } = 1.5;
    }
}
