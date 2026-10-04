namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 좌우 플래툰 (같은 손 대결일 때 타자 불리)
    /// </summary>
    public sealed class PlatoonConfig
    {
        /// <summary>같은 손 대결 컨택 로그 오즈 이동</summary>
        public double SameHandContactShift { get; set; } = -0.10;

        /// <summary>같은 손 대결 정타 로그 오즈 이동</summary>
        public double SameHandSolidShift { get; set; } = -0.10;

        /// <summary>같은 손 대결 존 밖 스윙 로그 오즈 이동</summary>
        public double SameHandChaseShift { get; set; } = 0.05;
    }
}
