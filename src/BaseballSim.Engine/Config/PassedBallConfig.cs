namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 폭투·포일·낫아웃 (포수가 공을 놓치는 확률)
    /// </summary>
    public sealed class PassedBallConfig
    {
        /// <summary>원바운드 판정: 존 하단보다 이만큼 아래로 온 공 (m)</summary>
        public double DirtBelowZoneM { get; set; } = 0.30;

        /// <summary>크게 빠진 공 판정: 존 반폭 바깥으로 이만큼 벗어난 공 (m)</summary>
        public double WideOutsideZoneM { get; set; } = 0.45;

        /// <summary>원바운드·크게 빠진 공을 포수가 놓칠 확률 (블로킹 50). 놓치면 폭투</summary>
        public double WildPitchRate { get; set; } = 0.20;

        /// <summary>정상 범위 공을 포수가 놓칠 확률 (블로킹 50). 놓치면 포일</summary>
        public double PassedBallRate { get; set; } = 0.003;

        /// <summary>포수 블로킹 1 표준편차당 놓칠 확률 로그 오즈 감소</summary>
        public double BlockingBeta { get; set; } = 0.30;

        /// <summary>낫아웃 때 타자가 1루에 살아갈 확률 (스피드 50)</summary>
        public double DroppedThirdStrikeReachRate { get; set; } = 0.35;

        /// <summary>스피드 1 표준편차당 낫아웃 출루 로그 오즈 증가</summary>
        public double DroppedThirdStrikeSpeedBeta { get; set; } = 0.30;
    }
}
