namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 투수 경기 내 피로
    /// 피로도 = (투구 수 − 무피로 투구 수) / 피로 누적 구간, 0~1로 제한
    /// </summary>
    public sealed class FatigueConfig
    {
        /// <summary>스태미나 50 투수의 무피로 투구 수</summary>
        public double ComfortPitchesBase { get; set; } = 62;

        /// <summary>스태미나 1 표준편차당 무피로 투구 수 변화</summary>
        public double ComfortPitchesPerSd { get; set; } = 18;

        /// <summary>무피로 구간 이후 피로도 1에 도달하기까지의 투구 수</summary>
        public double FadePitches { get; set; } = 30;

        /// <summary>피로도 1일 때 구속 감소 (km/h). 원본 약 2 mph</summary>
        public double VelocityDropKmh { get; set; } = 3.2;

        /// <summary>피로도 1일 때 제구 오차 증가 비율</summary>
        public double ControlSigmaIncrease { get; set; } = 0.35;

        /// <summary>피로도 1일 때 실효 구위 z 감소</summary>
        public double StuffZDrop { get; set; } = 0.6;
    }
}
