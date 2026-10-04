namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 수비 도구 × 포지션 숙련도 → 실효 수비 능력치
    /// 실효 = 20 + (도구 − 20) × 숙련도 배율
    /// 숙련도 배율: 숙련도 20일 때 MultiplierAtMin, FullProficiency일 때 1.0, 80일 때 MultiplierAtMax (선형 보간)
    /// </summary>
    public sealed class DefenseConfig
    {
        /// <summary>숙련도가 없는 포지션의 기본 숙련도</summary>
        public int DefaultProficiency { get; set; } = 25;

        /// <summary>도구를 온전히 발휘하는 숙련도</summary>
        public int FullProficiency { get; set; } = 60;

        public double MultiplierAtMin { get; set; } = 0.55;

        public double MultiplierAtMax { get; set; } = 1.10;
    }
}
