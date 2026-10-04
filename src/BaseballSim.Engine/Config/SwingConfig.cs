namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 투구 인지와 AI 타자 스윙 판단
    /// </summary>
    public sealed class SwingConfig
    {
        /// <summary>선구안 50 타자의 위치 인지 오차 표준편차 (축별, m)</summary>
        public double PerceptionSigmaM { get; set; } = 0.06;

        /// <summary>선구안 1 표준편차당 인지 오차 로그 배율 감소</summary>
        public double EyePerceptionBeta { get; set; } = 0.25;

        /// <summary>실효 구위 1 표준편차당 인지 오차 로그 배율 증가 (속임수)</summary>
        public double StuffPerceptionBeta { get; set; } = 0.15;

        /// <summary>카운트별 스윙 로그 오즈 이동</summary>
        public CountTable CountSwingShift { get; set; } = new CountTable(
            -0.70, 0.05, 0.35,
            -0.15, 0.15, 0.45,
            -0.25, 0.25, 0.55,
            -2.20, 0.05, 0.75);

        /// <summary>선구안 1 표준편차당 존 안(인지) 스윙 로그 오즈 증가</summary>
        public double EyeZoneSwingBeta { get; set; } = 0.05;

        /// <summary>선구안 1 표준편차당 존 밖(인지) 스윙 로그 오즈 감소</summary>
        public double EyeChaseBeta { get; set; } = 0.25;

        /// <summary>실효 구위 1 표준편차당 존 밖(인지) 스윙 로그 오즈 증가</summary>
        public double StuffChaseBeta { get; set; } = 0.15;
    }
}
