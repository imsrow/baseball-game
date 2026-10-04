namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 감독 AI 판단 기준 (A단계: 투수 교체만)
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
    }
}
