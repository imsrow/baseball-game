namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 스트라이크 존, 공략 구역, 볼 판정, 몸에 맞는 공 판정
    /// 좌표: x는 포수 시점 좌우(1루 쪽 +), z는 지면으로부터 높이. 단위 m.
    /// </summary>
    public sealed class StrikeZoneConfig
    {
        /// <summary>존 반폭 (공 반지름 포함). 원본 0.83 ft</summary>
        public double HalfWidthM { get; set; } = 0.253;

        /// <summary>존 하단 높이. 원본 1.6 ft</summary>
        public double BottomM { get; set; } = 0.488;

        /// <summary>존 상단 높이. 원본 3.4 ft</summary>
        public double TopM { get; set; } = 1.036;

        /// <summary>Heart 경계 (존 반폭·반높이 대비 비율). Statcast: 67%</summary>
        public double HeartLimit { get; set; } = 0.67;

        /// <summary>Shadow 바깥 경계. Statcast: 133%</summary>
        public double ShadowLimit { get; set; } = 1.33;

        /// <summary>Chase 바깥 경계. Statcast: 200%</summary>
        public double ChaseLimit { get; set; } = 2.0;

        /// <summary>
        /// 심판 판정 경계의 흐림 정도 (m). 존 경계에서 이 거리만큼 벗어날 때 로그 오즈가 1 변한다.
        /// 원본 약 0.8 inch
        /// </summary>
        public double UmpireEdgeScaleM { get; set; } = 0.02;

        /// <summary>포수 프레이밍 1 표준편차당 Shadow 구역 스트라이크 판정 로그 오즈 이동</summary>
        public double FramingBeta { get; set; } = 0.20;

        /// <summary>홈플레이트 중심에서 몸쪽으로 이 거리를 넘으면 몸에 맞을 수 있다 (m)</summary>
        public double HitByPitchInsideLineM { get; set; } = 0.40;

        /// <summary>몸에 맞을 수 있는 높이 하한 (m)</summary>
        public double HitByPitchMinHeightM { get; set; } = 0.15;

        /// <summary>몸에 맞을 수 있는 높이 상한 (m)</summary>
        public double HitByPitchMaxHeightM { get; set; } = 1.85;

        public double CenterHeightM => (BottomM + TopM) / 2.0;

        public double HalfHeightM => (TopM - BottomM) / 2.0;
    }
}
